using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class SetReplyHandler : AMessageHandler
    {
        internal static readonly SetReplyHandler Instance = new SetReplyHandler();
        private SetReplyHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var replyInfo = (SetReplyPacket)obj;
            return null;
        }
    }
}
