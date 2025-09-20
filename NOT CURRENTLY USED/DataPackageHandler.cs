/*using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;

namespace RnSArchipelago.Handler
{
    internal sealed class DataPackageHandler : AMessageHandler
    {
        internal static readonly DataPackageHandler Instance = new DataPackageHandler();
        private DataPackageHandler() { }

        internal override ArchipelagoPacketBase[]? Consume(ArchipelagoPacketBase obj, ILoggerV1 logger, IRNSReloaded rnsReloaded, Config.Config config)
        {
            var dataInfo = (DataPackagePacket)obj;
            if (dataInfo.DataPackage.Games.TryGetValue("Rabbit and Steel", out var data))
            {
                ArchipelagoConfig.Instance.setLocations(data.LocationLookup);
                ArchipelagoConfig.Instance.setItems(data.ItemLookup);
            }
            return null;
        }
    }
}
*/