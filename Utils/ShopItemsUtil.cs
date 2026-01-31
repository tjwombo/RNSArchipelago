using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Utils
{
    internal unsafe class ShopItemsUtil
    {
        private readonly Random rand;
        private readonly ILogger logger;
        private readonly InventoryUtil inventoryUtil;
        
        public ShopItemsUtil(Random rand, ILogger logger, InventoryUtil inventoryUtil)
        {
            this.rand = rand;
            this.logger = logger;
            this.inventoryUtil = inventoryUtil;
        }
        
        private static readonly string[] SHOP_LOCATIONS = ["Full Heal Potion Slot", "Level Up Slot", "Potion 1 Slot", "Potion 2 Slot", "Potion 3 Slot",
                  "Primary Upgrade Slot", "Secondary Upgrade Slot", "Special Upgrade Slot", "Defensive Upgrade Slot"];

        private static long PotionNameToId(string potion)
        {
            return potion switch
            {
                "Full Heal Potion" => 487,
                "Level Up Potion" => 488,
                "Regen Potion" => 489,
                "Essence of Spell" => 490,
                "Darkness Potion" => 491,
                "Quickening Potion" => 492,
                "Winged Potion" => 493,
                "Essence of Wit" => 494,
                "Swifthand Potion" => 495,
                "Fire Potion" => 496,
                "Strength Potion" => 497,
                "Gold Potion" => 498,
                "Luck Potion" => 499,
                "Essence of Steel" => 500,
                "Evasion Potion" => 501,
                "Longarm Potion" => 502,
                "Vitality Potion" => 503,
                _ => 0
            };
        }

        internal void SetHpPotion(RValue** argv, long archipelagoItemId, bool useArchipelago)
        {
            if (useArchipelago)
            {
                *argv[0] = new RValue(archipelagoItemId);
            } else if (this.inventoryUtil.PotionSanity == InventoryUtil.PotionSetting.None) {
                return;
            } else if (this.inventoryUtil.PotionSanity == InventoryUtil.PotionSetting.Locked) {
                if (this.inventoryUtil.AvailablePotions.Contains("Full Heal Potion"))
                {
                    *argv[0] = new RValue(PotionNameToId("Full Heal Potion"));
                } else
                {
                    *argv[0] = new RValue(0);
                }
            } else
            {
                if (this.inventoryUtil.AvailablePotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = this.inventoryUtil.AvailablePotions[rand.Next(this.inventoryUtil.AvailablePotions.Count)];
                    *argv[0] = new RValue(PotionNameToId(randomPotion));
                }
            }
        }

        internal void SetLevelPotion(RValue** argv, long archipelagoItemId, bool useArchipelago)
        {
            if (useArchipelago)
            {
                *argv[0] = new RValue(archipelagoItemId);
            }
            else if (this.inventoryUtil.PotionSanity == InventoryUtil.PotionSetting.None)
            {
                return;
            }
            else if (this.inventoryUtil.PotionSanity == InventoryUtil.PotionSetting.Locked)
            {
                if (this.inventoryUtil.AvailablePotions.Contains("Level Up Potion"))
                {
                    *argv[0] = new RValue(PotionNameToId("Level Up Potion"));
                }
                else
                {
                    *argv[0] = new RValue(0);
                }
            }
            else
            {
                if (this.inventoryUtil.AvailablePotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = this.inventoryUtil.AvailablePotions[rand.Next(this.inventoryUtil.AvailablePotions.Count)];
                    *argv[0] = new RValue(PotionNameToId(randomPotion));
                }
            }
        }

        internal void SetPotion(RValue** argv, long archipelagoItemId, bool useArchipelago)
        {
            if (useArchipelago)
            {
                *argv[0] = new RValue(archipelagoItemId);
            }
            else if (this.inventoryUtil.PotionSanity == InventoryUtil.PotionSetting.None)
            {
                return;
            }
            else if (this.inventoryUtil.PotionSanity == InventoryUtil.PotionSetting.Locked) 
            {
                List<string> actualPotions = this.inventoryUtil.AvailablePotions.Where(potion => (potion != "Full Heal Potion" && potion != "Level Up Potion")).ToList();
                logger?.PrintMessage(String.Join(", ", actualPotions), System.Drawing.Color.DarkOrange);
                if (actualPotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = actualPotions[rand.Next(actualPotions.Count)];
                    *argv[0] = new RValue(PotionNameToId(randomPotion));
                }
            } else
            {
                if (this.inventoryUtil.AvailablePotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = this.inventoryUtil.AvailablePotions[rand.Next(this.inventoryUtil.AvailablePotions.Count)];
                    *argv[0] = new RValue(PotionNameToId(randomPotion));
                }
            }
        }

        internal void SetPrimaryUpgrade(RValue** argv, long archipelagoItemId, bool useArchipelago)
        {
            if (useArchipelago)
            {
                *argv[0] = new RValue(archipelagoItemId);
            }
            else if (this.inventoryUtil.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {

                if (this.inventoryUtil.AvailablePrimaryUpgrades == InventoryUtil.PrimaryUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.PrimaryUpgradeFlags[] availablePrimary = Enum.GetValues(typeof(InventoryUtil.PrimaryUpgradeFlags)).
                    Cast<InventoryUtil.PrimaryUpgradeFlags>().
                    Where(x => this.inventoryUtil.AvailablePrimaryUpgrades.HasFlag(x) && x != InventoryUtil.PrimaryUpgradeFlags.None).ToArray();

                InventoryUtil.PrimaryUpgradeFlags randomPrimary = availablePrimary[rand.Next(availablePrimary.Length)];

                logger?.PrintMessage(String.Join(" ", availablePrimary.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryEmeraldGem)
                {
                    *argv[0] = new RValue(520);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryGarnetGem)
                {
                    *argv[0] = new RValue(516);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryRubyGem)
                {
                    *argv[0] = new RValue(512);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimarySapphireGem)
                {
                    *argv[0] = new RValue(508);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryOpalGem)
                {
                    *argv[0] = new RValue(504);
                }
            }
            
        }

        internal void SetSecondaryUpgrade(RValue** argv, long archipelagoItemId, bool useArchipelago)
        {
            if (useArchipelago)
            {
                *argv[0] = new RValue(archipelagoItemId);
            }
            else if (this.inventoryUtil.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {
                if (this.inventoryUtil.AvailableSecondaryUpgrades == InventoryUtil.SecondaryUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.SecondaryUpgradeFlags[] availableSecondary = Enum.GetValues(typeof(InventoryUtil.SecondaryUpgradeFlags)).
                    Cast<InventoryUtil.SecondaryUpgradeFlags>().
                    Where(x => this.inventoryUtil.AvailableSecondaryUpgrades.HasFlag(x) && x != InventoryUtil.SecondaryUpgradeFlags.None).ToArray();

                InventoryUtil.SecondaryUpgradeFlags randomSecondary = availableSecondary[rand.Next(availableSecondary.Length)];

                logger?.PrintMessage(String.Join(" ", availableSecondary.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryEmeraldGem)
                {
                    *argv[0] = new RValue(521);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryGarnetGem)
                {
                    *argv[0] = new RValue(517);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryRubyGem)
                {
                    *argv[0] = new RValue(513);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondarySapphireGem)
                {
                    *argv[0] = new RValue(509);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryOpalGem)
                {
                    *argv[0] = new RValue(505);
                }
            }

        }

        internal void SetSpecialUpgrade(RValue** argv, long archipelagoItemId, bool useArchipelago)
        {
            if (useArchipelago)
            {
                *argv[0] = new RValue(archipelagoItemId);
            }
            else if (this.inventoryUtil.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {
                if (this.inventoryUtil.AvailableSpecialUpgrades == InventoryUtil.SpecialUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.SpecialUpgradeFlags[] availableSpecial = Enum.GetValues(typeof(InventoryUtil.SpecialUpgradeFlags)).
                    Cast<InventoryUtil.SpecialUpgradeFlags>().
                    Where(x => this.inventoryUtil.AvailableSpecialUpgrades.HasFlag(x) && x != InventoryUtil.SpecialUpgradeFlags.None).ToArray();

                InventoryUtil.SpecialUpgradeFlags randomSpecial = availableSpecial[rand.Next(availableSpecial.Length)];

                logger?.PrintMessage(String.Join(" ", availableSpecial.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialEmeraldGem)
                {
                    *argv[0] = new RValue(522);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialGarnetGem)
                {
                    *argv[0] = new RValue(518);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialRubyGem)
                {
                    *argv[0] = new RValue(514);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialSapphireGem)
                {
                    *argv[0] = new RValue(510);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialOpalGem)
                {
                    *argv[0] = new RValue(506);
                }
            }

        }

        internal void SetDefensiveUpgrade(RValue** argv, long archipelagoItemId, bool useArchipelago)
        {
            if (useArchipelago)
            {
                *argv[0] = new RValue(archipelagoItemId);
            }
            else if (this.inventoryUtil.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {
                if (this.inventoryUtil.AvailableDefensiveUpgrades == InventoryUtil.DefensiveUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.DefensiveUpgradeFlags[] availableDefensive = Enum.GetValues(typeof(InventoryUtil.DefensiveUpgradeFlags)).
                    Cast<InventoryUtil.DefensiveUpgradeFlags>().
                    Where(x => this.inventoryUtil.AvailableDefensiveUpgrades.HasFlag(x) && x != InventoryUtil.DefensiveUpgradeFlags.None).ToArray();

                InventoryUtil.DefensiveUpgradeFlags randomDefensive = availableDefensive[rand.Next(availableDefensive.Length)];

                logger?.PrintMessage(String.Join(" ", availableDefensive.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveEmeraldGem)
                {
                    *argv[0] = new RValue(523);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveGarnetGem)
                {
                    *argv[0] = new RValue(519);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveRubyGem)
                {
                    *argv[0] = new RValue(515);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveSapphireGem)
                {
                    *argv[0] = new RValue(511);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveOpalGem)
                {
                    *argv[0] = new RValue(507);
                }
            }

        }
    }
}
