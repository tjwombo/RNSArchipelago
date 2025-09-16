/*using System.Text.Json;
using System.Text.Json.Serialization;
using RnSArchipelago.Utils.NetworkUtil;
using RnSArchipelago.Dispatcher;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Enums;
using RnSArchipelago.Config;
using System.Xml.Linq;

namespace RnSArchipelago.Handler
{
    internal sealed class RoomInfoHandler : AMessageHandler
    {
        internal static readonly RoomInfoHandler Instance = new RoomInfoHandler();
        private RoomInfoHandler() { }

        internal static readonly NetworkVersion VERSION = new NetworkVersion(0, 6, 3);
        internal static readonly string GAME = "Rabbit and Steel";

        internal override ArchipelagoPacketBase[]? Consume(ArchipelagoPacketBase obj, ILoggerV1 logger, IRNSReloaded rnsReloaded, Config.Config config)
        {
            var roomInfo = (RoomInfoPacket)obj;

            (var name, _, _, var password) = ArchipelagoConfig.Instance.getConfig();

            var connect = new ConnectPacket();
            if (roomInfo.Password)
            {
                connect.Password = password;
            }
            connect.Game = GAME;
            connect.Name = name;

            if (File.Exists(config.Cache + "\\common.json"))
            {
                TextReader textReader = File.OpenText(config.Cache + "\\common.json");
                var cache = JsonSerializer.Deserialize<JsonElement>(textReader.ReadToEnd());
                if (cache.TryGetProperty("uuid", out var uuid))
                {
                    connect.Uuid = uuid.GetString()!;
                }
                else
                {
                    Console.WriteLine("Uuid cannot be found");
                }
            }
            else
            {
                Console.WriteLine("cache cannot be found");
            }
            connect.Version = VERSION;

            connect.ItemsHandling = ItemsHandlingFlags.AllItems;
            connect.Tags = ["AP"]; // Read from settings (or roominfo?) to get deathlink
            connect.RequestSlotData = true;

            if (roomInfo.DataPackageChecksums.TryGetValue("Rabbit and Steel", out var checksum))
            {
                if (File.Exists(config.Cache + "\\datapackage\\Rabbit and Steel\\" + checksum + ".json"))
                {
                    TextReader textReader = File.OpenText(config.Cache + "\\datapackage\\Rabbit and Steel\\" + checksum + ".json");
                    var cache = JsonSerializer.Deserialize<JsonElement>(textReader.ReadToEnd());
                    if (cache.TryGetProperty("location_name_to_id", out var locations))
                    {
                        ArchipelagoConfig.Instance.setLocations(locations.Deserialize<Dictionary<string, long>>()!);
                    }
                    if (cache.TryGetProperty("item_name_to_id", out var items))
                    {
                        ArchipelagoConfig.Instance.setItems(items.Deserialize<Dictionary<string, long>>()!);
                    }


                    Thread.Sleep(100);
                    return [connect];
                }
            }

            var data = new GetDataPackagePacket
            {
                Games = ["Rabbit and Steel"]
            };
            return [data, connect];

        }
    }
}
*/