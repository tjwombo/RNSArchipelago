using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class BouncedHandler : AMessageHandler
    {
        internal static readonly BouncedHandler Instance = new BouncedHandler();
        private BouncedHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {

            var bouncedInfo = (BouncedPacket)obj;
            return null;
        }
    }
}
