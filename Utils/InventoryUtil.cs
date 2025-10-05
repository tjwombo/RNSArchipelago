using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using RnSArchipelago.Data;

namespace RnSArchipelago.Utils
{
    internal class InventoryUtil
    {
        private static readonly InventoryUtil _instance = new InventoryUtil();

        internal static InventoryUtil Instance => _instance;

        internal bool isActive;
        internal bool isKingdomSanity;
        //internal bool isOutskirtsShuffled;
        internal bool isProgressive;
        internal long maxKingdoms;
        internal long seed;
        internal Dictionary<int, List<string>> kingdomOrder = [];

        private InventoryUtil() => Reset();

        internal void Reset()
        {
            isActive = false;
            AvailableKingdoms = KingdomFlags.None;
            ProgressiveRegions = 0;
            kingdomOrder = [];
        }

        // Init function to get the kingdom options the user has selected
        internal void GetKingdomOptions(SharedData data)
        {
            isKingdomSanity = data.GetValue<long>(DataContext.Options, "kingdom_sanity") == 1;
            isProgressive = data.GetValue<long>(DataContext.Options, "progressive_regions") == 1;
            maxKingdoms = data.GetValue<long>(DataContext.Options, "max_kingdoms_per_run")!;
            seed = data.GetValue<long>(DataContext.Options, "seed");

            if (!isKingdomSanity)
            {
                AvailableKingdoms = KingdomFlags.All;
            }

            var kingdomOrderDict = data.GetValue<JObject>(DataContext.Options, "kingdom_order")!.ToObject<Dictionary<string, int>>();
            kingdomOrder = [];
            foreach (var entry in kingdomOrderDict!)
            {
                if (!kingdomOrder.ContainsKey(entry.Value-1))
                {
                    kingdomOrder[entry.Value-1] = [KingdomNameToNotch(entry.Key)];
                }
                else
                {

                    kingdomOrder[entry.Value - 1].Add(KingdomNameToNotch(entry.Key));
                }
            }
        }

        [Flags]
        internal enum KingdomFlags
        { 
            None = 0b00000000,
            Outskirts = 0b00000001,
            Scholars_Nest = 0b00000010,
            Kings_Arsenal = 0b00000100,
            Red_Darkhouse = 0b00001000,
            Churchmouse_Streets = 0b00010000,
            Emerald_Lakeside = 0b00100000,
            The_Pale_Keep = 0b01000000,
            Moonlit_Pinnacle = 0b10000000,
            All = 0b11111111
        }

        private static readonly string[] KINGDOMS = ["Kingdom Outskirts", "Scholar's Nest", "King's Arsenal", "Red Darkhouse", "Churchmouse Streets", "Emerald Lakeside", "The Pale Keep", "Moonlit Pinnacle"];

        internal KingdomFlags AvailableKingdoms { get; set; }
        internal int ProgressiveRegions { get; set; }

        // TODO: CREATE A SUBSCRIPTION OF SORTS TO UPDATE HOOKS IN REAL TIME WHEN NEEDED
        // Handle receiving kingdom related items
        internal void ReceiveItem(ReceivedItemsPacket recievedItem, SharedData data)
        {
            foreach (var item in recievedItem.Items)
            {
                var itemName = data.GetValue<string>(DataContext.IdToItem, item.Item);
                if (itemName != default)
                {
                    if (KINGDOMS.Contains(itemName))
                    {
                        AvailableKingdoms = AvailableKingdoms | (KingdomFlags)Enum.Parse(typeof(KingdomFlags), itemName.Replace(" ", "_").Replace("'", ""));
                        Console.WriteLine(AvailableKingdoms);
                    } else if (itemName == "Progressive Region")
                    {
                        ProgressiveRegions++;
                        Console.WriteLine(ProgressiveRegions);
                    }
                }
                
            }
        }

        // Get the notchname from a kingdoms full name
        internal static string KingdomNameToNotch(string name)
        {
            if (name == "Scholar's Nest")
            {
                return "hw_nest";
            }
            if (name == "King's Arsenal")
            {
                return "hw_arsenal";
            }
            if (name == "Emerald Lakeside")
            {
                return "hw_lakeside";
            }
            if (name == "Churchmouse Streets")
            {
                return "hw_streets";
            }
            if (name == "Red Darkhouse")
            {
                return "hw_lighthouse";
            }
            return "";
        }

        // Get all the kingdoms that are visitable at kingdom number n
        internal List<string> GetKingdomsAvailableAtNthOrder(int n)
        {
            var kingdoms = new List<string>();

            for (var i = 0; i < n; i++)
            {
                kingdoms = [.. kingdoms, .. GetNthOrderKingdoms(i+1)];
            }

            return kingdoms;
        }

        // Get the kingdoms that are of order n
        internal List<string> GetNthOrderKingdoms(int n)
        {
            var kingdoms = new List<string>();
            foreach (var kingdom in kingdomOrder[n-1])
            {
                if (kingdom == "hw_nest" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                {
                    kingdoms.Add(kingdom);
                }
                if (kingdom == "hw_arsenal" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                {
                    kingdoms.Add(kingdom);
                }
                if (kingdom == "hw_lakeside" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                {
                    kingdoms.Add(kingdom);
                }
                if (kingdom == "hw_streets" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                {
                    kingdoms.Add(kingdom);
                }
                if (kingdom == "hw_lighthouse" && (AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                {
                    kingdoms.Add(kingdom);
                }
            }
            return kingdoms;
        }

        // Get the number of kingdoms that are visitable regardless of order
        internal int AvailableKingdomsCount()
        {
            int count = 0;
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
            {
                count++;
            }
            return count;
        }

        // TODO: LOOK INTO THE POSSIBILITY OF REMOVING THESE
        internal bool isPaleKeepAccessible()
        {
            return ((AvailableKingdoms & InventoryUtil.KingdomFlags.The_Pale_Keep)) != 0;// &&
        }

        internal bool isMoonlitPinnacleAccessible()
        {
            return ((AvailableKingdoms & InventoryUtil.KingdomFlags.Moonlit_Pinnacle)) != 0;// &&
        }

    }
}
