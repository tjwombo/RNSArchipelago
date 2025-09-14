using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class RetrievedHandler : AMessageHandler
    {
        internal static readonly RetrievedHandler Instance = new RetrievedHandler();
        private RetrievedHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var retrievedInfo = (RetrievedPacket)obj;
            return null;
        }
    }
}
