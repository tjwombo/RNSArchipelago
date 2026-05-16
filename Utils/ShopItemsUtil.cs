using Reloaded.Mod.Interfaces;
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
                "Full Heal Potion" => 679,
                "Level Up Potion" => 680,
                "Regen Potion" => 681,
                "Essence of Spell" => 682,
                "Darkness Potion" => 683,
                "Quickening Potion" => 684,
                "Winged Potion" => 685,
                "Essence of Wit" => 686,
                "Swifthand Potion" => 687,
                "Fire Potion" => 688,
                "Strength Potion" => 689,
                "Gold Potion" => 690,
                "Luck Potion" => 691,
                "Essence of Steel" => 692,
                "Evasion Potion" => 693,
                "Longarm Potion" => 694,
                "Vitality Potion" => 695,
                "Experimental Potion" => 696,
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
                logger.PrintMessage(String.Join(", ", actualPotions), System.Drawing.Color.DarkOrange);
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

                logger.PrintMessage(String.Join(" ", availablePrimary.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryEmeraldGem)
                {
                    *argv[0] = new RValue(713);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryGarnetGem)
                {
                    *argv[0] = new RValue(709);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryRubyGem)
                {
                    *argv[0] = new RValue(705);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimarySapphireGem)
                {
                    *argv[0] = new RValue(701);
                } else if (randomPrimary == InventoryUtil.PrimaryUpgradeFlags.PrimaryOpalGem)
                {
                    *argv[0] = new RValue(697);
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

                logger.PrintMessage(String.Join(" ", availableSecondary.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryEmeraldGem)
                {
                    *argv[0] = new RValue(714);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryGarnetGem)
                {
                    *argv[0] = new RValue(710);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryRubyGem)
                {
                    *argv[0] = new RValue(706);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondarySapphireGem)
                {
                    *argv[0] = new RValue(702);
                }
                else if (randomSecondary == InventoryUtil.SecondaryUpgradeFlags.SecondaryOpalGem)
                {
                    *argv[0] = new RValue(698);
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

                logger.PrintMessage(String.Join(" ", availableSpecial.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialEmeraldGem)
                {
                    *argv[0] = new RValue(715);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialGarnetGem)
                {
                    *argv[0] = new RValue(711);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialRubyGem)
                {
                    *argv[0] = new RValue(707);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialSapphireGem)
                {
                    *argv[0] = new RValue(703);
                }
                else if (randomSpecial == InventoryUtil.SpecialUpgradeFlags.SpecialOpalGem)
                {
                    *argv[0] = new RValue(699);
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

                logger.PrintMessage(String.Join(" ", availableDefensive.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveEmeraldGem)
                {
                    *argv[0] = new RValue(716);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveGarnetGem)
                {
                    *argv[0] = new RValue(712);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveRubyGem)
                {
                    *argv[0] = new RValue(708);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveSapphireGem)
                {
                    *argv[0] = new RValue(704);
                }
                else if (randomDefensive == InventoryUtil.DefensiveUpgradeFlags.DefensiveOpalGem)
                {
                    *argv[0] = new RValue(700);
                }
            }

        }
    }
}
