using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Data;

namespace RnSArchipelago.Utils
{
    internal class InventoryUtil
    {
        private static readonly InventoryUtil _instance = new InventoryUtil();

        internal static InventoryUtil Instance => _instance;

        internal ILoggerV1? logger;

        internal bool isActive;
        internal bool isKingdomSanity;
        //internal bool isOutskirtsShuffled;
        internal bool isProgressive;
        internal bool useKingdomOrderWithKingdomSanity;
        internal long maxKingdoms;
        internal long seed;
        internal Dictionary<int, List<string>> kingdomOrder = [];

        internal bool isClassSanity;
        internal List<string> checksPerClass = [];
        internal bool shuffleItemsets;
        private List<long> availableItems = [];
        internal bool checksPerItemInChest;
        private List<string> availablePotions = [];
        private GoalSetting goal = GoalSetting.Shira;
        internal long shiraKills;
        private HashSet<string> victories = [];
        private ShopSetting shop_sanity = ShopSetting.None;

        internal delegate void UpdateKingdomRouteDelegate(bool currentHallwayPosAware = true);
        internal event UpdateKingdomRouteDelegate? UpdateKingdomRoute;

        internal delegate void AddChestDelegate();
        internal event AddChestDelegate? AddChest;

        internal delegate void SendGoalDelegate();
        internal event SendGoalDelegate? SendGoal;

        internal enum UpgradeSetting
        {
            None = 0,
            Simple = 1,
            Full = 2
        }

        internal UpgradeSetting UpgradeSanity { get; set; }

        internal enum PotionSetting
        {
            None = 0,
            Locked = 1,
            Roulette = 2
        }

        internal PotionSetting PotionSanity { get; set; }

        internal enum GoalSetting
        {
            Shira = 1
        }

        internal GoalSetting Goal => goal;

        internal enum ShopSetting
        {
            None = 0,
            Global = 1,
            Regional = 2
        }

        internal ShopSetting ShopSanity => shop_sanity;



        private InventoryUtil() => Reset();

        internal void Reset()
        {
            isActive = false;
            AvailableKingdoms = KingdomFlags.None;
            ProgressiveRegions = 0;
            kingdomOrder = [];
            AvailableClasses = ClassFlags.None;
            checksPerClass = [];
            availableItems = [];
            AvailableTreasurespheres = 0;
            UpgradeSanity = UpgradeSetting.None;
            PotionSanity = PotionSetting.None;
            availablePotions = [];
            goal = GoalSetting.Shira;
            shiraKills = 0;
            victories = [];
            shop_sanity = ShopSetting.None;
        }

        // Init function to get the options the user has selected
        internal void GetOptions(SharedData data)
        {
            isActive = true;

            isKingdomSanity = data.GetValue<long>(DataContext.Options, "kingdom_sanity") == 1;
            isProgressive = data.GetValue<long>(DataContext.Options, "progressive_regions") == 1;
            useKingdomOrderWithKingdomSanity = data.GetValue<long>(DataContext.Options, "kingdom_sanity_kingdom_order") == 1;
            maxKingdoms = data.GetValue<long>(DataContext.Options, "max_kingdoms_per_run")!;
            seed = data.GetValue<long>(DataContext.Options, "seed");

            if (!isKingdomSanity)
            {
                AvailableKingdoms = KingdomFlags.All;

                List<string> excluded_kingdoms = data.GetValue<JArray>(DataContext.Options, "excluded_kingdoms")?.ToObject<List<string>>()!;

                foreach (var kingdom in excluded_kingdoms)
                {
                    AvailableKingdoms = AvailableKingdoms & ~(KingdomFlags)Enum.Parse(typeof(KingdomFlags), kingdom.Replace(" ", "_").Replace("'", ""));
                }
            }

            var kingdomOrderDict = data.GetValue<JObject>(DataContext.Options, "kingdom_order")?.ToObject<Dictionary<string, int>>();
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

            isClassSanity = data.GetValue<long>(DataContext.Options, "class_sanity") == 1;

            if (!isClassSanity)
            {
                AvailableClasses = ClassFlags.All;
            }

            checksPerClass = data.GetValue<JArray>(DataContext.Options, "checks_per_class")?.ToObject<List<string>>()!;
            this.logger?.PrintMessage(String.Join(", ", checksPerClass), System.Drawing.Color.DarkOrange);

            shuffleItemsets = data.GetValue<long>(DataContext.Options, "shuffle_item_sets") == 1;
            checksPerItemInChest = data.GetValue<long>(DataContext.Options, "checks_per_item_in_chest") == 1;

            UpgradeSanity = (UpgradeSetting)data.GetValue<long>(DataContext.Options, "upgrade_sanity");
            this.logger?.PrintMessage(UpgradeSanity.ToString(), System.Drawing.Color.DarkOrange);

            PotionSanity = (PotionSetting)data.GetValue<long>(DataContext.Options, "potion_sanity");
            this.logger?.PrintMessage(PotionSanity.ToString(), System.Drawing.Color.DarkOrange);

            goal = (GoalSetting)data.GetValue<long>(DataContext.Options, "goal_condition");
            this.logger?.PrintMessage(goal.ToString(), System.Drawing.Color.DarkOrange);

            shiraKills = data.GetValue<long>(DataContext.Options, "shira_defeats")!;

            shop_sanity = (ShopSetting)data.GetValue<long>(DataContext.Options, "shop_sanity");
            this.logger?.PrintMessage(shop_sanity.ToString(), System.Drawing.Color.DarkOrange);
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

        [Flags]
        internal enum ClassFlags
        {
            None = 0b0000000000,
            Wizard = 0b0000000001,
            Assassin = 0b0000000010,
            Heavyblade = 0b0000000100,
            Dancer = 0b0000001000,
            Druid = 0b0000010000,
            Spellsword = 0b0000100000,
            Sniper = 0b0001000000,
            Bruiser = 0b0010000000,
            Defender = 0b0100000000,
            Ancient = 0b1000000000,
            All = 0b1111111111
        }

        private static readonly string[] CLASSES = ["Wizard", "Assassin", "Heavyblade", "Dancer", "Druid", "Spellsword", "Sniper", "Bruiser", "Defender", "Ancient"];

        internal ClassFlags AvailableClasses { get; set; }

        internal int AvailableClassesCount => CLASSES.Length;

        private static readonly string[] ITEMSETS = [ "Arcane Set", "Night Set","Timespace Set", "Wind Set", "Bloodwolf Set", "Assassin Set", "Rockdragon Set", "Flame Set",
                                                    "Gem Set", "Lightning Set", "Shrine Set", "Lucky Set", "Life Set", "Poison Set", "Depth Set", "Darkbite Set", "Timegem Set",
                                                    "Youkai Set", "Haunted Set", "Gladiator Set", "Sparkblade Set", "Swiftflight Set", "Sacredflame Set", "Ruins Set", "Lakeshrine Set"];

        internal List<long> AvailableItems => availableItems;

        #region Itemsets
        private static readonly long[] ARCANE_SET = [287, 288, 289, 290, 291, 292, 293, 294];

        private static readonly long[] NIGHT_SET = [295, 296, 297, 298, 299, 300, 301, 302];

        private static readonly long[] TIMESPACE_SET = [303, 304, 305, 306, 307, 308, 309, 310];

        private static readonly long[] WIND_SET = [311, 312, 313, 314, 315, 316, 317, 318];

        private static readonly long[] BLOODWOLF_SET = [319, 320, 321, 322, 323, 324, 325, 326];

        private static readonly long[] ASSASSIN_SET = [327, 328, 329, 330, 331, 332, 333, 334];

        private static readonly long[] ROCKDRAGON_SET = [335, 336, 337, 338, 339, 340, 341, 342];

        private static readonly long[] FLAME_SET = [343, 344, 345, 346, 347, 348, 349, 350];

        private static readonly long[] GEM_SET = [351, 352, 353, 354, 355, 356, 357, 358];

        private static readonly long[] LIGHTNING_SET = [359, 360, 361, 362, 363, 364, 365, 366];

        private static readonly long[] SHRINE_SET = [367, 368, 369, 370, 371, 372, 373, 374];

        private static readonly long[] LUCKY_SET = [375, 376, 377, 378, 379, 380, 381, 382];

        private static readonly long[] LIFE_SET = [383, 384, 385, 386, 387, 388, 389, 390];

        private static readonly long[] POISON_SET = [391, 392, 393, 394, 395, 396, 397, 398];

        private static readonly long[] DEPTH_SET = [399, 400, 401, 402, 403, 404, 405, 406];

        private static readonly long[] DARKBITE_SET = [407, 408, 409, 410, 411, 412, 413, 414];

        private static readonly long[] TIMEGEM_SET = [415, 416, 417, 418, 419, 420, 421, 422];

        private static readonly long[] YOUKAI_SET = [423, 424, 425, 426, 427, 428, 429, 430];

        private static readonly long[] HAUNTED_SET = [431, 432, 433, 434, 435, 436, 437, 438];

        private static readonly long[] GLADIATOR_SET = [439, 440, 441, 442, 443, 444, 445, 446];

        private static readonly long[] SPARKBLADE_SET = [447, 448, 449, 450, 451, 452, 453, 454];

        private static readonly long[] SWIFTFLIGHT_SET = [455, 456, 457, 458, 459, 460, 461,462];

        private static readonly long[] SACREDFLAME_SET = [463, 464, 465, 466, 467, 468, 469, 470];

        private static readonly long[] RUINS_SET = [471, 472, 473, 474, 475, 476, 477, 478];

        private static readonly long[] LAKESHRINE_SET = [479, 480, 481, 482, 483, 484, 485, 486];
        #endregion

        internal int AvailableTreasurespheres;

        internal static readonly string[] UPGRADES = ["Emerald Gem", "Garnet Gem", "Ruby Gem", "Sapphire Gem", "Opal Gem",
            "Primary Emerald Gem", "Primary Garnet Gem", "Primary Ruby Gem", "Primary Sapphire Gem", "Primary Opal Gem",
            "Secondary Emerald Gem", "Secondary Garnet Gem", "Secondary Ruby Gem", "Secondary Sapphire Gem", "Secondary Opal Gem",
            "Special Emerald Gem", "Special Garnet Gem", "Special Ruby Gem", "Special Sapphire Gem", "Special Opal Gem",
            "Defensive Emerald Gem", "Defensive Garnet Gem", "Defensive Ruby Gem", "Defensive Sapphire Gem", "Defensive Opal Gem"];

        [Flags]
        internal enum PrimaryUpgradeFlags
        {
            None = 0,
            PrimaryEmeraldGem = 0x00001,
            PrimaryGarnetGem = 0x00010,
            PrimaryRubyGem = 0x00100,
            PrimarySapphireGem = 0x01000,
            PrimaryOpalGem = 0x10000,
        }

        internal PrimaryUpgradeFlags AvailablePrimaryUpgrades { get; set; }

        [Flags]
        internal enum SecondaryUpgradeFlags {
            None = 0,
            SecondaryEmeraldGem = 0x00001,
            SecondaryGarnetGem = 0x00010,
            SecondaryRubyGem = 0x00100,
            SecondarySapphireGem = 0x01000,
            SecondaryOpalGem = 0x10000,
        }

        internal SecondaryUpgradeFlags AvailableSecondaryUpgrades { get; set; }

        [Flags]
        internal enum SpecialUpgradeFlags {
            None = 0,
            SpecialEmeraldGem = 0x00001,
            SpecialGarnetGem = 0x00010,
            SpecialRubyGem = 0x00100,
            SpecialSapphireGem = 0x01000,
            SpecialOpalGem = 0x10000,
        }

        internal SpecialUpgradeFlags AvailableSpecialUpgrades { get; set; }

        [Flags]
        internal enum DefensiveUpgradeFlags { 
            None = 0,
            DefensiveEmeraldGem = 0x00001,
            DefensiveGarnetGem = 0x00010,
            DefensiveRubyGem = 0x00100,
            DefensiveSapphireGem = 0x01000,
            DefensiveOpalGem = 0x10000,
        }

        internal DefensiveUpgradeFlags AvailableDefensiveUpgrades { get; set; }

        internal static readonly string[] POTIONS = ["Full Heal Potion", "Level Up Potion", "Regen Potion", "Essence of Spell", "Darkness Potion", "Quickening Potion", "Winged Potion",
            "Essence of Wit", "Swifthand Potion", "Fire Potion", "Strength Potion", "Gold Potion", "Luck Potion", "Essence of Steel", "Evasion Potion", "Longarm Potion", "Vitality Potion"];

        internal List<string> AvailablePotions => availablePotions;

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
                        this.logger?.PrintMessage(AvailableKingdoms.ToString(), System.Drawing.Color.DarkOrange);
                        UpdateKingdomRoute?.Invoke();
                    } 
                    else if (itemName == "Progressive Region")
                    {
                        ProgressiveRegions++;
                        this.logger?.PrintMessage(ProgressiveRegions + "", System.Drawing.Color.DarkOrange);
                        UpdateKingdomRoute?.Invoke();
                    } 
                    else if (CLASSES.Contains(itemName))
                    {
                        AvailableClasses = AvailableClasses | (ClassFlags)Enum.Parse(typeof(ClassFlags), itemName);
                        this.logger?.PrintMessage(AvailableClasses.ToString(), System.Drawing.Color.DarkOrange);
                    } 
                    else if (ITEMSETS.Contains(itemName))
                    {
                        AddItemsFromItemset(itemName);
                        this.logger?.PrintMessage(String.Join(", ", AvailableItems), System.Drawing.Color.DarkOrange);
                    } 
                    else if (itemName == "Treasuresphere")
                    {
                        AddChest?.Invoke();
                        AvailableTreasurespheres++;
                    } 
                    else if (UPGRADES.Contains(itemName))
                    {
                        var enumName = itemName.Replace(" ", "");
                        if (InventoryUtil.Instance.UpgradeSanity == InventoryUtil.UpgradeSetting.Simple)
                        {
                            if (enumName.Contains("Emerald"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryEmeraldGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryEmeraldGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialEmeraldGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveEmeraldGem;
                            } else if (enumName.Contains("Garnet"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryGarnetGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryGarnetGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialGarnetGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveGarnetGem;
                            } else if (enumName.Contains("Ruby"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryRubyGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryRubyGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialRubyGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveRubyGem;
                            } else if (enumName.Contains("Sapphire"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimarySapphireGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondarySapphireGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialSapphireGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveSapphireGem;
                            } else if (enumName.Contains("Opal"))
                            {
                                AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | PrimaryUpgradeFlags.PrimaryOpalGem;
                                AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | SecondaryUpgradeFlags.SecondaryOpalGem;
                                AvailableSpecialUpgrades = AvailableSpecialUpgrades | SpecialUpgradeFlags.SpecialOpalGem;
                                AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | DefensiveUpgradeFlags.DefensiveOpalGem;
                            }
                        } else if (enumName.Contains("Primary"))
                        {
                            AvailablePrimaryUpgrades = AvailablePrimaryUpgrades | (PrimaryUpgradeFlags)Enum.Parse(typeof(PrimaryUpgradeFlags), enumName);
                            this.logger?.PrintMessage(AvailablePrimaryUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        } else if (enumName.Contains("Secondary"))
                        {
                            AvailableSecondaryUpgrades = AvailableSecondaryUpgrades | (SecondaryUpgradeFlags)Enum.Parse(typeof(SecondaryUpgradeFlags), enumName);
                            this.logger?.PrintMessage(AvailableSecondaryUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        } else if (enumName.Contains("Special"))
                        {
                            AvailableSpecialUpgrades = AvailableSpecialUpgrades | (SpecialUpgradeFlags)Enum.Parse(typeof(SpecialUpgradeFlags), enumName);
                            this.logger?.PrintMessage(AvailableSpecialUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        } else if (enumName.Contains("Defensive"))
                        {
                            AvailableDefensiveUpgrades = AvailableDefensiveUpgrades | (DefensiveUpgradeFlags)Enum.Parse(typeof(DefensiveUpgradeFlags), enumName);
                            this.logger?.PrintMessage(AvailableDefensiveUpgrades.ToString(), System.Drawing.Color.DarkOrange);
                        }
                    } 
                    else if (POTIONS.Contains(itemName))
                    {
                        AvailablePotions.Add(itemName);
                        this.logger?.PrintMessage(String.Join(", ", AvailablePotions), System.Drawing.Color.DarkOrange);
                    } else if (itemName.Contains("Victory"))
                    {
                        victories.Add(itemName);
                        this.logger?.PrintMessage(String.Join(", ", victories), System.Drawing.Color.DarkOrange);
                        if (CheckGoal())
                        {
                            SendGoal?.Invoke();
                        }
                    }
                }
                
            }
        }

        internal bool CheckGoal()
        {
            if (goal == GoalSetting.Shira)
            {
                if (victories.Count >= shiraKills)
                {
                    return true;
                }
            }
            return false;
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

            if (isKingdomSanity && !useKingdomOrderWithKingdomSanity)
            {
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                {
                    kingdoms.Add("hw_nest");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                {
                    kingdoms.Add("hw_arsenal");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                {
                    kingdoms.Add("hw_lakeside");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                {
                    kingdoms.Add("hw_streets");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                {
                    kingdoms.Add("hw_lighthouse");
                }
            }
            else
            {
                for (var i = 0; i < n; i++)
                {
                    kingdoms = [.. kingdoms, .. GetNthOrderKingdoms(i + 1)];
                }
            }

            return kingdoms;
        }

        // Get the kingdoms that are of order n
        internal List<string> GetNthOrderKingdoms(int n)
        {
            if (n <= 0)
            {
                return [];
            }
            var kingdoms = new List<string>();
            if (isKingdomSanity && !useKingdomOrderWithKingdomSanity)
            {
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                {
                    kingdoms.Add("hw_nest");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                {
                    kingdoms.Add("hw_arsenal");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                {
                    kingdoms.Add("hw_lakeside");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                {
                    kingdoms.Add("hw_streets");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                {
                    kingdoms.Add("hw_lighthouse");
                }
            }
            else if (isProgressive || (isKingdomSanity && useKingdomOrderWithKingdomSanity))
            {
                foreach (var kingdom in kingdomOrder[n - 1])
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
            } else
            {
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                {
                    kingdoms.Add("hw_nest");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                {
                    kingdoms.Add("hw_arsenal");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                {
                    kingdoms.Add("hw_lakeside");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                {
                    kingdoms.Add("hw_streets");
                }
                if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                {
                    kingdoms.Add("hw_lighthouse");
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

        internal bool isClassAvailable(int pos)
        {
            switch (pos)
            {
                case 0:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Wizard) != 0;
                case 1:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Assassin) != 0;
                case 2:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Heavyblade) != 0;
                case 3:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Dancer) != 0;
                case 4:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Druid) != 0;
                case 5:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Spellsword) != 0;
                case 6:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Sniper) != 0;
                case 7:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Bruiser) != 0;
                case 8:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Defender) != 0;
                case 9:
                    return (AvailableClasses & InventoryUtil.ClassFlags.Ancient) != 0;
            }
            return false;
        }

        internal string GetClass(int pos)
        {
            return CLASSES[pos];
        }

        private void AddItemsFromItemset(string itemset)
        {
            switch (itemset) {
                case "Arcane Set":
                    availableItems.AddRange(ARCANE_SET);
                    break;
                case "Night Set":
                    availableItems.AddRange(NIGHT_SET);
                    break;
                case "Timespace Set":
                    availableItems.AddRange(TIMESPACE_SET);
                    break;
                case "Wind Set":
                    availableItems.AddRange(WIND_SET);
                    break;
                case "Bloodwolf Set":
                    availableItems.AddRange(BLOODWOLF_SET);
                    break;
                case "Assassin Set":
                    availableItems.AddRange(ASSASSIN_SET);
                    break;
                case "Rockdragon Set":
                    availableItems.AddRange(ROCKDRAGON_SET);
                    break;
                case "Flame Set":
                    availableItems.AddRange(FLAME_SET);
                    break;
                case "Gem Set":
                    availableItems.AddRange(GEM_SET);
                    break;
                case "Lightning Set":
                    availableItems.AddRange(LIGHTNING_SET);
                    break;
                case "Shrine Set":
                    availableItems.AddRange(SHRINE_SET);
                    break;
                case "Lucky Set":
                    availableItems.AddRange(LUCKY_SET);
                    break;
                case "Life Set":
                    availableItems.AddRange(LIFE_SET);
                    break;
                case "Poison Set":
                    availableItems.AddRange(POISON_SET);
                    break;
                case "Depth Set":
                    availableItems.AddRange(DEPTH_SET);
                    break;
                case "Darkbite Set":
                    availableItems.AddRange(DARKBITE_SET);
                    break;
                case "Timegem Set":
                    availableItems.AddRange(TIMEGEM_SET);
                    break;
                case "Youkai Set":
                    availableItems.AddRange(YOUKAI_SET);
                    break;
                case "Haunted Set":
                    availableItems.AddRange(HAUNTED_SET);
                    break;
                case "Gladiator Set":
                    availableItems.AddRange(GLADIATOR_SET);
                    break;
                case "Sparkblade Set":
                    availableItems.AddRange(SPARKBLADE_SET);
                    break;
                case "Swiftflight Set":
                    availableItems.AddRange(SWIFTFLIGHT_SET);
                    break;
                case "Sacredflame Set":
                    availableItems.AddRange(SACREDFLAME_SET);
                    break;
                case "Ruins Set":
                    availableItems.AddRange(RUINS_SET);
                    break;
                case "Lakeshrine Set":
                    availableItems.AddRange(LAKESHRINE_SET);
                    break;
            }
        }
    }
}
