using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Archipelago.MultiClient.Net.Packets;
using static System.Collections.Specialized.BitVector32;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace RnSArchipelago.Builder
{
    internal sealed class ConnectBuilder : AMessageBuilder
    {
        internal static readonly ConnectBuilder Instance = new ConnectBuilder();
        
        internal static readonly string PATH = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Archipelago\\Cache\\common.json";
        internal static readonly NetworkVersion VERSION = new NetworkVersion(0, 6, 3);
        internal static readonly string GAME = "Ocarina of Time";

        private ConnectBuilder() { }

        internal override ArchipelagoPacketBase Build(ArchipelagoPacketBase obj)
        {
            var info = (RoomInfoPacket)obj;

            (var name, _, _, var password) = ArchipelagoConfig.Instance.getConfig();

            ConnectPacket output = new ConnectPacket();
            //output.Command = "Connect";
            if (info.Password)
            {
                output.Password = password;
            }
            output.Game = GAME;
            output.Name = name;
            
            if (File.Exists(PATH))
            {
                TextReader textReader = File.OpenText(PATH);
                var cache = JsonSerializer.Deserialize<JsonElement>(textReader.ReadToEnd());
                if (cache.TryGetProperty("uuid", out var uuid))
                {
                    output.Uuid = uuid.GetString()!;
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
            output.Version = VERSION;

            output.ItemsHandling = ItemsHandlingFlags.AllItems;
            output.Tags = new string[] { "AP" }; // Read from settings (or roominfo?) to get deathlink
            output.RequestSlotData = true;

            return output;
            
        }
    }
}
