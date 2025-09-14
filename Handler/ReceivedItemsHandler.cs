using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class ReceivedItemsHandler : AMessageHandler
    {
        internal static readonly ReceivedItemsHandler Instance = new ReceivedItemsHandler();
        private ReceivedItemsHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var itemsInfo = (ReceivedItemsPacket)obj;
            return null;
        }
    }
}
