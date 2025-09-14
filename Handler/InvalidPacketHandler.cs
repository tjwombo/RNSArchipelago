using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class InvalidPacketHandler : AMessageHandler
    {
        internal static readonly InvalidPacketHandler Instance = new InvalidPacketHandler();
        private InvalidPacketHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var packetInfo = (InvalidPacketPacket)obj;
            return null;
        }
    }
}
