using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RnSArchipelago.Utils
{
    namespace NetworkUtil
    {

        /*internal class NetworkPlayer
        {
            [JsonInclude]
            [JsonPropertyName("team")]
            internal int Team { get; set; }

            [JsonInclude]
            [JsonPropertyName("slot")]
            internal int Slot { get; set; }

            [JsonInclude]
            [JsonPropertyName("alias")]
            internal string Alias { get; set; }

            [JsonInclude]
            [JsonPropertyName("name")]
            internal string Name { get; set; }

            [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            internal NetworkPlayer() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            public override string ToString()
            {
                return "{team: " + Team
                    + ", slot: " + Slot
                    + ", alias: " + Alias
                    + ", name: " + Name + "}";
            }
        }*/

        /*internal class NetworkItem
        {
            [JsonInclude]
            [JsonPropertyName("item")]
            internal int Item { get; set; }

            [JsonInclude]
            [JsonPropertyName("location")]
            internal int Location { get; set; }

            [JsonInclude]
            [JsonPropertyName("player")]
            internal int Player { get; set; }

            [JsonInclude]
            [JsonPropertyName("flags")]
            internal ItemFlags Flags { get; set; }

            [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            internal NetworkItem() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            public override string ToString()
            {
                return "{item: " + Item
                    + ", location: " + Location
                    + ", player: " + Player
                    + ", flags: " + Flags + "}";
            }
        }

        internal class JsonMessagePart
        {
            [JsonInclude]
            [JsonPropertyName("type")]
            internal string? Type { get; set; }

            [JsonInclude]
            [JsonPropertyName("text")]
            internal string? Text { get; set; }

            [JsonInclude]
            [JsonPropertyName("color")]
            internal string? Color { get; set; }

            [JsonInclude]
            [JsonPropertyName("flags")]
            internal ItemFlags? Flags { get; set; }

            [JsonInclude]
            [JsonPropertyName("player")]
            internal int? Player { get; set; }

            [JsonInclude]
            [JsonPropertyName("hint_status")]
            internal HintStatus? Hint { get; set; }

            [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            internal JsonMessagePart() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            public override string ToString()
            {
                return "{type: " + Type
                    + ", text: " + Text
                    + ", color: " + Color
                    + ", flags: " + Flags
                    + ", player: " + Player
                    + ", hint: " + Hint + "}";
            }
        }

        internal class NetworkSlot
        {
            [JsonInclude]
            [JsonPropertyName("name")]
            internal string Name { get; set; }

            [JsonInclude]
            [JsonPropertyName("game")]
            internal string Game { get; set; }

            [JsonInclude]
            [JsonPropertyName("type")]
            internal SlotType Type { get; set; }

            [JsonInclude]
            [JsonPropertyName("group_members")]
            internal List<int> GroupMembers { get; set; }

            [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            internal NetworkSlot() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            public override string ToString()
            {
                return "{name: " + Name
                    + ", game: " + Game
                    + ", type: " + Type
                    + ", members: " + string.Join(", ", GroupMembers) + "}";
            }
        }

        internal class Hint
        {
            [JsonInclude]
            [JsonPropertyName("receiving_player")]
            internal int ReceivingPlayer { get; set; }

            [JsonInclude]
            [JsonPropertyName("finding_player")]
            internal int FindingPlayer { get; set; }

            [JsonInclude]
            [JsonPropertyName("location")]
            internal int Location { get; set; }

            [JsonInclude]
            [JsonPropertyName("item")]
            internal int Item { get; set; }

            [JsonInclude]
            [JsonPropertyName("found")]
            internal bool Found { get; set; }

            [JsonInclude]
            [JsonPropertyName("entrance")]
            internal string Entrance { get; set; }

            [JsonInclude]
            [JsonPropertyName("item_flags")]
            internal ItemFlags ItemFlags { get; set; }

            [JsonInclude]
            [JsonPropertyName("status")]
            internal HintStatus Status { get; set; }

            [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            internal Hint() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            public override string ToString()
            {
                return "{receiving: " + ReceivingPlayer
                    + ", finding: " + FindingPlayer
                    + ", location: " + Location
                    + ", item: " + Item
                    + ", found: " + Found
                    + ", entrance: " + Entrance
                    + ", flags: " + ItemFlags
                    + ", status: " + Status + "}";
            }
        }

        internal class DataPackageObject
        {
            [JsonInclude]
            [JsonPropertyName("games")]
            internal Dictionary<string, GameData> Games { get; set; }

            [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            internal DataPackageObject() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            public override string ToString()
            {
                return "games: " + string.Join(", ", Games.Select(entry => $"{entry.Key}:{entry.Value}"));
            }
        }

        internal class GameData
        {
            [JsonInclude]
            [JsonPropertyName("item_name_to_id")]
            internal Dictionary<string, int> ItemNameToId { get; set; }

            [JsonInclude]
            [JsonPropertyName("location_name_to_id")]
            internal Dictionary<string, int> LocationNameToId { get; set; }

            [JsonInclude]
            [JsonPropertyName("checksum")]
            internal string Checksum { get; set; }

            [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            internal GameData() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            public override string ToString()
            {
                return "{items: " + string.Join(", ", ItemNameToId.Select(entry => $"{entry.Key}:{entry.Value}"))
                    + ", locations: " + string.Join(", ", LocationNameToId.Select(entry => $"{entry.Key}:{entry.Value}"))
                    + ", checksum: " + Checksum + "}";
            }
        }*/
    }
}
