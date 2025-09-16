using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using System.Text.Json;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;

namespace RnSArchipelago.Connection
{
    internal class ArchipelagoConnection
    {
        private IRNSReloaded rnsReloaded;
        private ILoggerV1 logger;
        private IReloadedHooks hooks;
        private Config.Config modConfig;

        internal IHook<ScriptDelegate>? startConnectionHook;

        private ArchipelagoSession session;

        internal static readonly NetworkVersion VERSION = new NetworkVersion(0, 6, 3);
        internal static readonly string GAME = "Rabbit and Steel";

        internal ArchipelagoConnection(IRNSReloaded rnsReloaded, ILoggerV1 logger, IReloadedHooks hooks, Config.Config config)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
            this.hooks = hooks;
            this.modConfig = config;
        }

        // Attempt to start a connection to archipelago with the given configs
        internal async void StartConnection()
        {
            var config = ArchipelagoConfig.Instance;
            (_, string address, _, _) = config.getConfig();


            session = ArchipelagoSessionFactory.CreateSession(address);

            var message = MessageHandler.Instance;
            message.rnsReloaded = rnsReloaded;
            message.logger = logger;
            message.modConfig = modConfig;

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

        internal void ConnectionOpened()
        {
            //ServerCommandDispatcher.Instance.Session = session;
            Console.WriteLine("connection opened");
            //session.Socket.SendPacketAsync(new LocationChecksPacket { Locations = [1, 2, 3, 4, 5] });
        }

        internal void ConnectionClosed(string reason)
        {
            logger.PrintMessage(reason, System.Drawing.Color.Red);
            //session.Socket.PacketReceived -= ServerCommandDispatcher.Instance.Dispatch;
            session.MessageLog.OnMessageReceived -= MessageHandler.Instance.OnMessageReceived;
            session.Socket.SocketOpened -= ConnectionOpened;
            session.Socket.SocketClosed -= ConnectionClosed;
        }

        internal async void ErrorReceived(Exception e, string message)
        {
            logger.PrintMessage(message, System.Drawing.Color.Red);
            await session.Socket.DisconnectAsync();
        }

        internal void JoinRoom(RoomInfoPacket roomInfo)
        {
            (var name, _, _, var password) = ArchipelagoConfig.Instance.getConfig();

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
                        ArchipelagoConfig.Instance.setLocations(locations.Deserialize<Dictionary<string, long>>()!);
                    }
                    if (cache.TryGetProperty("item_name_to_id", out var items))
                    {
                        ArchipelagoConfig.Instance.setItems(items.Deserialize<Dictionary<string, long>>()!);
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
