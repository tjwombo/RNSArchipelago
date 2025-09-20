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

namespace RnSArchipelago.Connection
{
    internal class ArchipelagoConnection
    {
        private IRNSReloaded rnsReloaded;
        private ILoggerV1 logger;
        private IReloadedHooks hooks;
        private Config.Config modConfig;
        private SharedData data;

        internal IHook<ScriptDelegate>? startConnectionHook;

        internal ArchipelagoSession session;

        private static readonly NetworkVersion VERSION = new NetworkVersion(0, 6, 3);
        private static readonly string GAME = "Rabbit and Steel";

        internal ArchipelagoConnection(IRNSReloaded rnsReloaded, ILoggerV1 logger, IReloadedHooks hooks, Config.Config config, SharedData data)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
            this.hooks = hooks;
            this.modConfig = config;
            this.data = data;
        }

        // Attempt to start a connection to archipelago with the given configs
        internal async void StartConnection()
        {

            var address = data.GetValue<string>(DataContext.Connection, "address");

            if (session == null)
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

                    return;
                }
                catch (Exception e)
                {
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

        internal async void ResetConn()
        {
            InventoryUtil.Instance.ResetItems();
            this.logger.PrintMessage("reseting", System.Drawing.Color.Red);
            data.SetValue<string>(DataContext.Connection, "name", default);
            data.SetValue<string>(DataContext.Connection, "address", default);
            data.SetValue<string>(DataContext.Connection, "numPlayers", default);
            data.SetValue<string>(DataContext.Connection, "password", default);
            await this.session.Socket.DisconnectAsync();
            this.session = null;
        }

        internal void ConnectionOpened()
        {
            Console.WriteLine("connection opened");
        }

        internal void ConnectionClosed(string reason)
        {
            logger.PrintMessage("Connection closed: " + reason, System.Drawing.Color.Red);
            session.MessageLog.OnMessageReceived -= MessageHandler.Instance.OnMessageReceived;
            if (session != null)
            {
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
            connect.Tags = ["AP"]; // Read from settings (or roominfo?) to get deathlink
            connect.RequestSlotData = true;

            var locationPacket = new LocationChecksPacket { Locations = [1, 2, 3, 4, 5] };

            Thread.Sleep(100);

            if (roomInfo.DataPackageChecksums.TryGetValue("Rabbit and Steel", out var checksum))
            {
                if (File.Exists(modConfig.Cache + "\\datapackage\\Rabbit and Steel\\" + checksum + ".json"))
                {
                    TextReader textReader = File.OpenText(modConfig.Cache + "\\datapackage\\Rabbit and Steel\\" + checksum + ".json");
                    var cache = JsonSerializer.Deserialize<JsonElement>(textReader.ReadToEnd());
                    if (cache.TryGetProperty("location_name_to_id", out var locations))
                    {
                        var locationId = locations.Deserialize<Dictionary<string, long>>()!;
                        foreach (var location in locationId) {
                            this.data.SetValue<long>(DataContext.LocationToId, location.Key, location.Value);
                            this.data.SetValue<string>(DataContext.IdToLocation, location.Value, location.Key);
                        }
                    }
                    if (cache.TryGetProperty("item_name_to_id", out var items))
                    {
                        var itemId = items.Deserialize<Dictionary<string, long>>()!;
                        foreach (var item in itemId)
                        {
                            this.data.SetValue<long>(DataContext.ItemToId, item.Key, item.Value);
                            this.data.SetValue<string>(DataContext.IdToItem, item.Value, item.Key);
                        }
                    }

                    session.Socket.SendMultiplePacketsAsync(new List<ArchipelagoPacketBase>() { connect, locationPacket });
                    return;
                }
            }

            var data = new GetDataPackagePacket
            {
                Games = ["Rabbit and Steel"]
            };
            session.Socket.SendMultiplePacketsAsync(new List<ArchipelagoPacketBase>() { data, connect, locationPacket });
        }

    }
}
