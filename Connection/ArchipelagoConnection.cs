using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;
using RnSArchipelago.Data;
using RnSArchipelago.Utils;
using RnSArchipelago.Game;
using System.Diagnostics.CodeAnalysis;
using Reloaded.Mod.Interfaces;

namespace RnSArchipelago.Connection
{
    internal class ArchipelagoConnection
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryUtil inventoryUtil;
        private readonly Config.Config modConfig;
        private readonly SharedData data;

        internal readonly MessageHandler messageHandler;

        internal IHook<ScriptDelegate>? resetConnHook;
        internal IHook<ScriptDelegate>? resetConnEndHook;

        internal ArchipelagoSession? session;

        private static readonly NetworkVersion VERSION = new(0, 6, 3);
        private static readonly string GAME = "Rabbit and Steel";

        internal ArchipelagoConnection(
            WeakReference<IRNSReloaded> rnsReloadedRef,
            ILogger logger,
            InventoryUtil inventoryUtil,
            Config.Config config,
            SharedData data)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryUtil = inventoryUtil;
            this.modConfig = config;
            this.data = data;

            this.messageHandler = new MessageHandler(rnsReloadedRef, logger, inventoryUtil, config, data);
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

                    
                    if (returnToTitle)
                    {
                        this.messageHandler.errorMessage = "Disconnected from the multiworld";
                        ReturnToTitle();
                    }

                    return;
                }
            }
        }

        internal unsafe void ReturnToTitle()
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                rnsReloaded.ExecuteScript("scr_runmenu_disband_disband", null, null, []);
            }
        }

        // Return to the lobby settings
        internal unsafe RValue* ResetConn(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Reset Conn", System.Drawing.Color.DarkOrange);
            }
            this.resetConnHook?.OriginalFunction(self, other, returnValue, argc, argv);
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Rest Conn", System.Drawing.Color.DarkOrange);
            }
            ResetConn();
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Reset Conn", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Return to the lobby settings, after a win/loss
        internal unsafe RValue* ResetConnEnd(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Reset Conn End", System.Drawing.Color.DarkOrange);
            }
            this.resetConnEndHook?.OriginalFunction(self, other, returnValue, argc, argv);
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Reset Conn End", System.Drawing.Color.DarkOrange);
            }
            ResetConn();
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Reset Conn End", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Reset the archipelago connection and inventory
        internal void ResetConn()
        {
            this.inventoryUtil.Reset();

            data.connection.Set<string>("name", default!);
            data.connection.Set<string>("address", default!);
            data.connection.Set<string>("numPlayers", default!);
            data.connection.Set<string>("password", default!);

            if (this.session != null && this.session.Socket != null && this.session.Socket.Connected)
            {
                this.session.Socket.DisconnectAsync().Wait();
            }
        }

        internal void ConnectionOpened()
        {
            this.logger.PrintMessage("connection opened", System.Drawing.Color.DarkOrange);
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
            logger.PrintMessage("Error: " + message, System.Drawing.Color.Red);
            logger.PrintMessage(e + "", System.Drawing.Color.Red);

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
                catch (Exception err) {
                    logger.PrintMessage("Error in disconnecting: " + err, System.Drawing.Color.Red);
                    session = null;
                }
            }

            this.inventoryUtil.Reset();
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
            session?.Socket.SendMultiplePacketsAsync(new List<ArchipelagoPacketBase>() { data, connect}).Wait();
        }
    }
}
