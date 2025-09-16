/*using System.Text.Json.Serialization;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;


namespace RnSArchipelago.Handler
{
    internal sealed class ConnectionHandler : AMessageHandler
    {
        internal static readonly ConnectionHandler Instance = new ConnectionHandler();
        private ConnectionHandler() { }

        internal override ArchipelagoPacketBase[]? Consume(ArchipelagoPacketBase obj, ILoggerV1 logger, IRNSReloaded rnsReloaded, Config.Config config)
        {
            var connectionInfo = (ConnectedPacket)obj;
            var mapping = new Dictionary<long, Archipelago.MultiClient.Net.Models.NetworkPlayer>();
            foreach (var player in connectionInfo.Players)
            {
                mapping.Add(player.Slot, player);
                // IGNORES / ASSUMES THERE ARE NO TEAMS AT THE MOMENT AS I'M UNSURE HOW TEAMS WOULD AFFECT PRINTJSON
            }
            ArchipelagoConfig.Instance.ids_to_players = mapping;
            ArchipelagoConfig.Instance.player_id = connectionInfo.Slot;
            ArchipelagoConfig.Instance.ids_to_slot = connectionInfo.SlotInfo;
            // MIGHT BE WEIRD WITH GROUPS (AND MAYBE SPECTATORS BUT I DONT EXPECT IT TO BE)
            return null;
        }
    }
}
*/