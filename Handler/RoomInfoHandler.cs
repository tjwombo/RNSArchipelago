using System.Text.Json;
using System.Text.Json.Serialization;
using RnSArchipelago.Utils.NetworkUtil;
using RnSArchipelago.Dispatcher;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class RoomInfoHandler : AMessageHandler
    {
        internal static readonly RoomInfoHandler Instance = new RoomInfoHandler();
        private RoomInfoHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            Console.WriteLine("roomInfo");
            var roomInfo = (RoomInfoPacket)obj;

            if (false)
            {
                // I don't know when we would call this one yet
                var output = new ArchipelagoPacketBase[2];
                output[0] = ClientCommandDispatcher.Instance.BuildCommand("GetDataPackage", roomInfo);
                output[1] = ClientCommandDispatcher.Instance.BuildCommand("Connect", roomInfo);
                return output;
            } else
            {
                return new ArchipelagoPacketBase[] { ClientCommandDispatcher.Instance.BuildCommand("Connect", roomInfo)};
            }
        }
    }
}
