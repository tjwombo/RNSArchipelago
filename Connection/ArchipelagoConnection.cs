using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using System.Text.Json;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;
using RnSArchipelago.Data;
using RnSArchipelago.Utils;
using RnSArchipelago.Game;

namespace RnSArchipelago.Connection
{
    internal class ArchipelagoConnection
    {
        private readonly IRNSReloaded rnsReloaded;
        private readonly ILoggerV1 logger;
        private readonly Config.Config modConfig;
        private readonly SharedData data;

        internal IHook<ScriptDelegate>? resetConnHook;
        internal IHook<ScriptDelegate>? resetConnEndHook;

        internal ArchipelagoSession? session;
        internal LocationHandler locationHandler;

        private static readonly NetworkVersion VERSION = new(0, 6, 3);
        private static readonly string GAME = "Rabbit and Steel";

        internal ArchipelagoConnection(IRNSReloaded rnsReloaded, ILoggerV1 logger, Config.Config config, SharedData data, LocationHandler locationHandler)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
            this.modConfig = config;
            this.data = data;
            this.locationHandler = locationHandler;
        }

        // Attempt to start a connection to archipelago with the given configs
        internal async void StartConnection()
        {

            var address = data.GetValue<string>(DataContext.Connection, "address");

            if (session == null || !session.Socket.Connected)
            {

                session = ArchipelagoSessionFactory.CreateSession(address);
                var message = MessageHandler.Instance;
                message.rnsReloaded = rnsReloaded;
                message.logger = logger;
                message.modConfig = modConfig;
                message.data = data;

                session.Socket.PacketReceived += message.OnPacketReceived;
                session.MessageLog.OnMessageReceived += message.OnMessageReceived;
                session.Socket.SocketOpened += ConnectionOpened;
                session.Socket.ErrorReceived += ErrorReceived;
                session.Socket.SocketClosed += ConnectionClosed;

                try
                {
                    var roomInfo = await session.ConnectAsync();
                    JoinRoom(roomInfo!);
                    locationHandler.conn = this;
                    locationHandler.SendStartLocation();

                    return;
                }
                catch (Exception e)
                {
                    // TODO: gracefully handle bad connection, currently crashes
                    LoginFailure failure = new LoginFailure(e.GetBaseException().Message);
                    string errorMessage = $"Failed to Connect to {address}:";
                    foreach (string error in failure.Errors)
                    {
                        errorMessage += $"\n    {error}";
                    }
                    foreach (ConnectionRefusedError error in failure.ErrorCodes)
                    {
                        errorMessage += $"\n    {error}";
                    }
                    logger.PrintMessage(errorMessage, System.Drawing.Color.Red);
                    return;
                }
            }
        }

        // Return to the lobby settings
        internal unsafe RValue* ResetConn(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            this.resetConnHook!.OriginalFunction(self, other, returnValue, argc, argv);
            ResetConn();
            return returnValue;
        }

        // Return to the lobby settings, after a win/loss
        internal unsafe RValue* ResetConnEnd(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            this.resetConnEndHook!.OriginalFunction(self, other, returnValue, argc, argv);
            ResetConn();
            return returnValue;
        }

        // Reset the archipelago connection and inventory
        internal void ResetConn()
        {
            InventoryUtil.Instance.Reset();

            data.SetValue<string>(DataContext.Connection, "name", default!);
            data.SetValue<string>(DataContext.Connection, "address", default!);
            data.SetValue<string>(DataContext.Connection, "numPlayers", default!);
            data.SetValue<string>(DataContext.Connection, "password", default!);

            if (this.session != null && this.session.Socket != null && this.session.Socket.Connected)
            {
                var disconnect = this.session.Socket.DisconnectAsync();
                disconnect.Wait();
            }
        }

        internal void ConnectionOpened()
        {
            Console.WriteLine("connection opened");
        }

        internal void ConnectionClosed(string reason)
        {
            logger.PrintMessage("Connection closed: " + reason, System.Drawing.Color.Red);
            if (session != null && session.Socket != null)
            {
                session.MessageLog.OnMessageReceived -= MessageHandler.Instance.OnMessageReceived;
                session.Socket.SocketOpened -= ConnectionOpened;
                session.Socket.SocketClosed -= ConnectionClosed;
            }
        }

        internal async void ErrorReceived(Exception e, string message)
        {
            logger.PrintMessage("Error: " + message, System.Drawing.Color.Red);
            if (session != null)
            {
                await session.Socket.DisconnectAsync();
            }
        }

        // Attempt to join the archipelago room with the provided data
        internal void JoinRoom(RoomInfoPacket roomInfo)
        {

            var name = this.data.GetValue<string>(DataContext.Connection, "name");
            var password = this.data.GetValue<string>(DataContext.Connection, "password");

            var connect = new ConnectPacket();
            if (roomInfo.Password)
            {
                connect.Password = password;
            }
            connect.Game = GAME;
            connect.Name = name;

            if (File.Exists(modConfig.Cache + "\\common.json"))
            {
                TextReader textReader = File.OpenText(modConfig.Cache + "\\common.json");
                var cache = JsonSerializer.Deserialize<JsonElement>(textReader.ReadToEnd());
                if (cache.TryGetProperty("uuid", out var uuid))
                {
                    connect.Uuid = uuid.GetString()!;
                }
                else
                {
                    Console.WriteLine("Uuid cannot be found");
                }
            }
            else
            {
                Console.WriteLine("cache cannot be found");
            }
            connect.Version = VERSION;

            connect.ItemsHandling = ItemsHandlingFlags.AllItems;
            connect.Tags = ["AP"]; // TODO: Read from settings (or roominfo?) to get deathlink
            connect.RequestSlotData = true;

            Thread.Sleep(100);

            // Don't request a datapackage if we have one
            if (roomInfo.DataPackageChecksums.TryGetValue("Rabbit and Steel", out var checksum))
            {
                if (File.Exists(modConfig.Cache + "\\datapackage\\Rabbit and Steel\\" + checksum + ".json"))
                {
                    // TODO: Look into getting rid of this, as we can get the info from the session
                    TextReader textReader = File.OpenText(modConfig.Cache + "\\datapackage\\Rabbit and Steel\\" + checksum + ".json");
                    var cache = JsonSerializer.Deserialize<JsonElement>(textReader.ReadToEnd());
                    if (cache.TryGetProperty("item_name_to_id", out var items))
                    {
                        var itemId = items.Deserialize<Dictionary<string, long>>()!;
                        foreach (var item in itemId)
                        {
                            this.data.SetValue<string>(DataContext.IdToItem, item.Value, item.Key);
                        }
                    }

                    session!.Socket.SendMultiplePacketsAsync(new List<ArchipelagoPacketBase>() { connect}).Wait();
                    return;
                }
            }

            var data = new GetDataPackagePacket
            {
                Games = ["Rabbit and Steel"]
            };
            session!.Socket.SendMultiplePacketsAsync(new List<ArchipelagoPacketBase>() { data, connect}).Wait();
        }
    }
}
