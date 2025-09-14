using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using RnSArchipelago.Handler;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

namespace RnSArchipelago.Dispatcher
{
    internal class ServerCommandDispatcher
    {
        private static readonly ServerCommandDispatcher _instance = new ServerCommandDispatcher();
        private readonly Dictionary<ArchipelagoPacketType, AMessageHandler> _handlerMap = new();
        internal ArchipelagoSession Session { get; set; }

        private ServerCommandDispatcher()
        {
            _handlerMap[ArchipelagoPacketType.RoomInfo] = RoomInfoHandler.Instance;
            _handlerMap[ArchipelagoPacketType.ConnectionRefused] = ConnectionRefusedHandler.Instance;
            _handlerMap[ArchipelagoPacketType.Connected] = ConnectionHandler.Instance;
            _handlerMap[ArchipelagoPacketType.ReceivedItems] = ReceivedItemsHandler.Instance;
            _handlerMap[ArchipelagoPacketType.LocationInfo] = LocationInfoHandler.Instance;
            _handlerMap[ArchipelagoPacketType.RoomUpdate] = RoomUpdateHandler.Instance;
            _handlerMap[ArchipelagoPacketType.PrintJSON] = PrintJsonHandler.Instance;
            _handlerMap[ArchipelagoPacketType.DataPackage] = DataPackageHandler.Instance;
            _handlerMap[ArchipelagoPacketType.Bounced] = BouncedHandler.Instance;
            _handlerMap[ArchipelagoPacketType.InvalidPacket] = InvalidPacketHandler.Instance;
            _handlerMap[ArchipelagoPacketType.Retrieved] = RetrievedHandler.Instance;
            _handlerMap[ArchipelagoPacketType.SetReply] = SetReplyHandler.Instance;
        }

        internal static ServerCommandDispatcher Instance => _instance;

        internal async void Dispatch(ArchipelagoPacketBase packet)
        {
            Console.WriteLine("dispatch: " + packet.PacketType);
            if (_handlerMap.TryGetValue(packet.PacketType, out var handler))
            {
                var packets = handler.Consume(packet);
                if (packets != null)
                {
                    await Session.Socket.SendMultiplePacketsAsync(packets);
                }
            }

            //return new SocketResult(SocketStatusCode.badParse, null);
        }
    }
}
