/*using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;

namespace RnSArchipelago.Handler
{
    internal sealed class ReceivedItemsHandler : AMessageHandler
    {
        internal static readonly ReceivedItemsHandler Instance = new ReceivedItemsHandler();
        private ReceivedItemsHandler() { }

        internal override ArchipelagoPacketBase[]? Consume(ArchipelagoPacketBase obj, ILoggerV1 logger, IRNSReloaded rnsReloaded, Config.Config config)
        {
            var itemsInfo = (ReceivedItemsPacket)obj;
            foreach (var item in itemsInfo.Items)
            {
                *//*if (ArchipelagoConfig.Instance.ids_to_items.TryGetValue(item.Item, out var name))
                {

                }*//*
            }
            return null;
        }
    }
}
*/