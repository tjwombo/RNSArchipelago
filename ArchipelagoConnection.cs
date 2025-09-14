using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RnSArchipelago.Dispatcher;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

namespace RnSArchipelago
{
    internal class ArchipelagoConnection
    {
        private IRNSReloaded rnsReloaded;
        private ILoggerV1 logger;
        private IReloadedHooks hooks;

        ArchipelagoConfig config;
        internal IHook<ScriptDelegate>? startConnectionHook;

        private ArchipelagoSession session;

        internal ArchipelagoConnection(IRNSReloaded rnsReloaded, ILoggerV1 logger, IReloadedHooks hooks)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
            this.hooks = hooks;
        }

        // Attempt to start a connection to archipelago with the given configs
        internal async void StartConnection()
        {
            config = ArchipelagoConfig.Instance;
            (_, string address, _, _) = config.getConfig();


            session = ArchipelagoSessionFactory.CreateSession(address);
            session.Socket.PacketReceived += ServerCommandDispatcher.Instance.Dispatch;
            session.Socket.SocketOpened += ConnectionOpened;
            session.Socket.SocketClosed += ConnectionClosed;

            try
            {
                await session.ConnectAsync();
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
                this.logger.PrintMessage(errorMessage, Color.Red);
                return;
            }

        }

        internal void ConnectionOpened()
        {
            ServerCommandDispatcher.Instance.Session = session;
        }

        internal void ConnectionClosed(string reason)
        {
            this.logger.PrintMessage(reason, Color.Red);
            session.Socket.PacketReceived -= ServerCommandDispatcher.Instance.Dispatch;
            session.Socket.SocketOpened -= ConnectionOpened;
            session.Socket.SocketClosed -= ConnectionClosed;
        }

    }
}
