using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using RnSArchipelago.Data;
using RnSArchipelago.Game;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Connection
{
    internal class ArchipelagoConnection
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private readonly SaveHandler saveHandler;
        private readonly SharedData data;
        private readonly Config.Config config;

        internal readonly MessageHandler messageHandler;

        internal IHook<ScriptDelegate>? resetConnHook;
        internal IHook<ScriptDelegate>? resetConnClientHook;
        internal IHook<ScriptDelegate>? recconectHook;
        internal IHook<ScriptDelegate>? parseSteamPacketHook;
        internal IHook<ScriptDelegate>? sendClientConnectionHook;
        internal IHook<ScriptDelegate>? sendClientHallwayHook;

        internal ArchipelagoSession? session;
        internal bool isHost = true;

        private static readonly NetworkVersion VERSION = new(0, 6, 7);
        internal static readonly string GAME = "Rabbit and Steel";

        internal ArchipelagoConnection(
            WeakReference<IRNSReloaded> rnsReloadedRef,
            ILogger logger,
            InventoryHandler inventoryHandler,
            SaveHandler saveHandler,
            ScoutHandler scoutHandler,
            Config.Config config,
            SharedData data)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.saveHandler = saveHandler;
            this.data = data;
            this.config = config;

            this.messageHandler = new MessageHandler(rnsReloadedRef, logger, inventoryHandler, scoutHandler, config, data);
        }

        // Send the connection command to the client joining
        internal unsafe RValue* SendClientConnection(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.sendClientConnectionHook != null)
            {
                returnValue = this.sendClientConnectionHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call send client connect hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var args = new RValue[] { new(60), new(1), new(1) }; // Create a buffer of initial size 60, set to grow, with 1 byte padding
                var buffer = rnsReloaded.ExecuteCodeFunction("buffer_create", null, null, args);

                var msg = new RValue();
                rnsReloaded.CreateString(&msg, "APConnect;" + config.StartUpConfig.ArchipelagoAddress + ";" + config.StartUpConfig.ArchipelagoName + ";" + config.StartUpConfig.ArchipelagoPassword);
                args = new RValue[] { buffer!.Value, new(11), msg}; // Treat the message as a string
                rnsReloaded.ExecuteCodeFunction("buffer_write", null, null, args);

                args = new RValue[] { *rnsReloaded.FindValue(self, "clientId"), buffer!.Value };
                rnsReloaded.ExecuteScript("scr_socket_send_tcp_to_client", self, other, args);

            }

            return returnValue;
        }

        // Send the updated hallway info to the clients
        internal unsafe RValue* SendClientHallway(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.sendClientHallwayHook != null)
            {
                returnValue = this.sendClientHallwayHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call send client hallway hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var args = new RValue[] { new(60), new(1), new(1) }; // Create a buffer of initial size 60, set to grow, with 1 byte padding
                var buffer = rnsReloaded.ExecuteCodeFunction("buffer_create", null, null, args);

                var msg = new RValue();
                rnsReloaded.CreateString(&msg, "APHallway;" + "");
                args = new RValue[] { buffer!.Value, new(11), msg }; // Treat the message as a string
                rnsReloaded.ExecuteCodeFunction("buffer_write", null, null, args);

                args = new RValue[] { *rnsReloaded.FindValue(self, "clientId"), buffer!.Value };
                rnsReloaded.ExecuteScript("scr_socket_send_tcp_to_client", self, other, args);

            }

            return returnValue;
        }

        // Handle steam packets for multiplayer communication
        internal unsafe RValue* ParseSteamPacket(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var buffer = *argv[1];

                // Make sure we're at the beginning of the buffer
                RValue[] args = [buffer, new(0), new(0)];
                rnsReloaded.ExecuteCodeFunction("buffer_seek", null, null, args);

                // Read the buffer as a string
                args = [buffer, new(11)]; // 11 is to read the buffer as a string
                var message = rnsReloaded.ExecuteCodeFunction("buffer_read", null, null, args);

                // Reset the position to the beginning for the actual read
                args = [buffer, new(0), new(0)];
                rnsReloaded.ExecuteCodeFunction("buffer_seek", null, null, args);

                if (message.HasValue)
                {
                    if (message.Value.ToString().StartsWith("APConnect"))
                    {
                        this.logger.PrintMessage(message.Value.ToString(), System.Drawing.Color.Red);
                        string[] parts = message.Value.ToString().Substring(10).Split(";");
                        this.logger.PrintMessage(parts.Length + " " + parts[0] + " " + message.Value.ToString().Substring(10), System.Drawing.Color.Red);

                        this.data.connection.Set<string>("address", parts[0]);
                        this.data.connection.Set<string>("name", parts[1]);
                        this.data.connection.Set<string>("password", parts[2]);
                        isHost = false;
                        _ = StartConnection(true);

                        saveHandler.UnlockSaveInfo();

                        *rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(3);
                    }
                }
            }

            if (this.parseSteamPacketHook != null)
            {
                returnValue = this.parseSteamPacketHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call parse steam packet hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Attempt to start a connection to archipelago with the given configs
        internal async Task StartConnection(bool returnToTitle = false)
        {
            var address = data.connection.Get<string>("address");

            if (session == null || !session.Socket.Connected)
            {
                session = ArchipelagoSessionFactory.CreateSession(address);

                if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
                {
                    session.Socket.PacketReceived += this.messageHandler.OnPacketReceived;
                    session.MessageLog.OnMessageReceived += this.messageHandler.OnMessageReceived;
                    session.Socket.SocketOpened += ConnectionOpened;
                    session.Socket.ErrorReceived += ErrorReceived;
                    session.Socket.SocketClosed += ConnectionClosed;
                }

                try
                {
                    var roomInfo = await session.ConnectAsync();
                    JoinRoom(roomInfo!);
                }
                catch (Exception e)
                {
                    LoginFailure failure = new(e.GetBaseException().Message);
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

                    if (returnToTitle)
                    {
                        this.messageHandler.errorMessage = "Could not connect to the multiworld";
                    }
                }
            }
        }

        // Return to the lobby settings
        internal unsafe RValue* ResetConn(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.resetConnHook != null)
            {
                this.resetConnHook?.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call reset conn hook", System.Drawing.Color.Red);
            }

            ResetConn();

            return returnValue;
        }

        // Return to the lobby list after leaving as client
        internal unsafe RValue* ResetConnClient(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.resetConnClientHook != null)
            {
                this.resetConnClientHook?.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call reset conn client hook", System.Drawing.Color.Red);
            }

            ResetConn();

            return returnValue;
        }

        // Reset the archipelago connection and inventory
        internal void ResetConn()
        {
            this.inventoryHandler.Reset();

            data.connection.Set<string>("name", default!);
            data.connection.Set<string>("address", default!);
            data.connection.Set<string>("numPlayers", default!);
            data.connection.Set<string>("password", default!);

            if (this.session != null && this.session.Socket != null)
            {
                if (this.session.Socket.Connected)
                {
                    this.session.Socket.DisconnectAsync().Wait();
                }

                session.Socket.PacketReceived -= this.messageHandler.OnPacketReceived;
                session.MessageLog.OnMessageReceived -= this.messageHandler.OnMessageReceived;
                session.Socket.SocketOpened -= ConnectionOpened;
                session.Socket.ErrorReceived -= ErrorReceived;
                session.Socket.SocketClosed -= ConnectionClosed;
            }

            this.session = null;
        }

        internal void ConnectionOpened()
        {
            this.logger.PrintMessage("Connection opened", System.Drawing.Color.DarkOrange);
        }

        internal void ConnectionClosed(string reason)
        {
            logger.PrintMessage("Connection closed: " + reason, System.Drawing.Color.Red);
            this.messageHandler.errorMessage = "Disconnected from the multiworld";
            if (session != null && session.Socket != null)
            {
                session.MessageLog.OnMessageReceived -= this.messageHandler.OnMessageReceived;
                session.Socket.SocketOpened -= ConnectionOpened;
                session.Socket.SocketClosed -= ConnectionClosed;
            }
        }

        internal async void ErrorReceived(Exception e, string message)
        {
            logger.PrintMessage(e + ": " + message, System.Drawing.Color.Red);

            if (message == "The remote party closed the WebSocket connection without completing the close handshake." ||
                message.Contains("Unable to connect to the remote server"))
            {
                this.messageHandler.errorMessage = "Disconnected from the multiworld";
                session = null;
            }

            if (session != null)
            {
                try
                {
                    await session.Socket.DisconnectAsync();
                }
                catch (Exception err)
                {
                    logger.PrintMessage("Error in disconnecting: " + err, System.Drawing.Color.Red);
                    session = null;
                }
            }

            this.inventoryHandler.Reset();
        }

        // Attempt to join the archipelago room with the provided data
        internal void JoinRoom(RoomInfoPacket roomInfo)
        {
            var name = this.data.connection.Get<string>("name");
            var password = this.data.connection.Get<string>("password");

            var connect = new ConnectPacket();
            if (roomInfo.Password)
            {
                connect.Password = password;
            }
            connect.Game = GAME;
            connect.Name = name;
            connect.Version = VERSION;

            connect.ItemsHandling = ItemsHandlingFlags.AllItems;
            connect.Tags = ["AP"]; // TODO: Read from settings (or roominfo?) to get deathlink
            connect.RequestSlotData = true;

            Thread.Sleep(100);

            var data = new GetDataPackagePacket
            {
                Games = ["Rabbit and Steel"]
            };
            session?.Socket.SendMultiplePacketsAsync(new List<ArchipelagoPacketBase>() { data, connect }).Wait();
        }

        internal unsafe RValue* Recconect(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                logger.PrintMessage("Attempting to Reconnect", System.Drawing.Color.Red);
                StartConnection().Wait();
                rnsReloaded.ExecuteScript("scr_runmenu_lobbysettings_return", null, null, []);

                return returnValue;
            }
            recconectHook?.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }
    }
}