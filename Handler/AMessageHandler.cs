using Archipelago.MultiClient.Net;

namespace RnSArchipelago.Handler
{
    internal abstract class AMessageHandler
    {
        internal abstract ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj);
    }
}
