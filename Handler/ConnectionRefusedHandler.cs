using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class ConnectionRefusedHandler : AMessageHandler
    {
        internal static readonly ConnectionRefusedHandler Instance = new ConnectionRefusedHandler();
        private ConnectionRefusedHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var connectionInfo = (ConnectionRefusedPacket)obj;
            return null;
        }
    }
}
