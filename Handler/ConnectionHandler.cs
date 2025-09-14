using System.Text.Json.Serialization;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;


namespace RnSArchipelago.Handler
{
    internal sealed class ConnectionHandler : AMessageHandler
    {
        internal static readonly ConnectionHandler Instance = new ConnectionHandler();
        private ConnectionHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var connectionInfo = (ConnectedPacket)obj;
            return null;
        }
    }
}
