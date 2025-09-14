using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class LocationInfoHandler : AMessageHandler
    {
        internal static readonly LocationInfoHandler Instance = new LocationInfoHandler();
        private LocationInfoHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var locationInfo = (LocationInfoPacket)obj;
            return null;
        }
    }
}
