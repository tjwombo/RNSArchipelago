using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RnSArchipelago.Handler
{
    internal sealed class DataPackageHandler : AMessageHandler
    {
        internal static readonly DataPackageHandler Instance = new DataPackageHandler();
        private DataPackageHandler() { }

        internal override ArchipelagoPacketBase[] Consume(ArchipelagoPacketBase obj)
        {
            var dataInfo = (DataPackagePacket)obj;
            return null;
        }
    }
}
