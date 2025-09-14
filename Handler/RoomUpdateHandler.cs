using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class RoomUpdateHandler : AMessageHandler
    {
        internal static readonly RoomUpdateHandler Instance = new RoomUpdateHandler();
        private RoomUpdateHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var roomInfo = (RoomUpdatePacket)obj;
            return null;
        }
    }
}
