using Reloaded.Mod.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class ShopItemsHandler
    {
        private readonly Random rand;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;

        public ShopItemsHandler(Random rand, ILogger logger, InventoryHandler inventoryHandler)
        {
            this.rand = rand;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
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
            }
            else if (inventoryHandler.PotionSanity == InventoryHandler.PotionSetting.None)
            {
                return;
            }
            else if (inventoryHandler.PotionSanity == InventoryHandler.PotionSetting.Locked)
            {
                if (inventoryHandler.AvailablePotions.Contains("Full Heal Potion"))
                {
                    *argv[0] = new RValue(PotionNameToId("Full Heal Potion"));
                }
                else
                {
                    *argv[0] = new RValue(0);
                }
            }
            else
            {
                if (inventoryHandler.AvailablePotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = inventoryHandler.AvailablePotions[rand.Next(inventoryHandler.AvailablePotions.Count)];
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
            else if (inventoryHandler.PotionSanity == InventoryHandler.PotionSetting.None)
            {
                return;
            }
            else if (inventoryHandler.PotionSanity == InventoryHandler.PotionSetting.Locked)
            {
                if (inventoryHandler.AvailablePotions.Contains("Level Up Potion"))
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
                if (inventoryHandler.AvailablePotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = inventoryHandler.AvailablePotions[rand.Next(inventoryHandler.AvailablePotions.Count)];
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
            else if (inventoryHandler.PotionSanity == InventoryHandler.PotionSetting.None)
            {
                return;
            }
            else if (inventoryHandler.PotionSanity == InventoryHandler.PotionSetting.Locked)
            {
                List<string> actualPotions = inventoryHandler.AvailablePotions.Where(potion => potion != "Full Heal Potion" && potion != "Level Up Potion").ToList();
                logger.PrintMessage(string.Join(", ", actualPotions), System.Drawing.Color.DarkOrange);
                if (actualPotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = actualPotions[rand.Next(actualPotions.Count)];
                    *argv[0] = new RValue(PotionNameToId(randomPotion));
                }
            }
            else
            {
                if (inventoryHandler.AvailablePotions.Count == 0)
                {
                    *argv[0] = new RValue(0);
                }
                else
                {
                    string randomPotion = inventoryHandler.AvailablePotions[rand.Next(inventoryHandler.AvailablePotions.Count)];
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
            else if (inventoryHandler.UpgradeSanity == InventoryHandler.UpgradeSetting.None)
            {
                return;
            }
            else
            {

                if (inventoryHandler.AvailablePrimaryUpgrades == InventoryHandler.PrimaryUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryHandler.PrimaryUpgradeFlags[] availablePrimary = Enum.GetValues(typeof(InventoryHandler.PrimaryUpgradeFlags)).
                    Cast<InventoryHandler.PrimaryUpgradeFlags>().
                    Where(x => inventoryHandler.AvailablePrimaryUpgrades.HasFlag(x) && x != InventoryHandler.PrimaryUpgradeFlags.None).ToArray();

                InventoryHandler.PrimaryUpgradeFlags randomPrimary = availablePrimary[rand.Next(availablePrimary.Length)];

                logger.PrintMessage(string.Join(" ", availablePrimary.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomPrimary == InventoryHandler.PrimaryUpgradeFlags.PrimaryEmeraldGem)
                {
                    *argv[0] = new RValue(713);
                }
                else if (randomPrimary == InventoryHandler.PrimaryUpgradeFlags.PrimaryGarnetGem)
                {
                    *argv[0] = new RValue(709);
                }
                else if (randomPrimary == InventoryHandler.PrimaryUpgradeFlags.PrimaryRubyGem)
                {
                    *argv[0] = new RValue(705);
                }
                else if (randomPrimary == InventoryHandler.PrimaryUpgradeFlags.PrimarySapphireGem)
                {
                    *argv[0] = new RValue(701);
                }
                else if (randomPrimary == InventoryHandler.PrimaryUpgradeFlags.PrimaryOpalGem)
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
            else if (inventoryHandler.UpgradeSanity == InventoryHandler.UpgradeSetting.None)
            {
                return;
            }
            else
            {
                if (inventoryHandler.AvailableSecondaryUpgrades == InventoryHandler.SecondaryUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryHandler.SecondaryUpgradeFlags[] availableSecondary = Enum.GetValues(typeof(InventoryHandler.SecondaryUpgradeFlags)).
                    Cast<InventoryHandler.SecondaryUpgradeFlags>().
                    Where(x => inventoryHandler.AvailableSecondaryUpgrades.HasFlag(x) && x != InventoryHandler.SecondaryUpgradeFlags.None).ToArray();

                InventoryHandler.SecondaryUpgradeFlags randomSecondary = availableSecondary[rand.Next(availableSecondary.Length)];

                logger.PrintMessage(string.Join(" ", availableSecondary.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomSecondary == InventoryHandler.SecondaryUpgradeFlags.SecondaryEmeraldGem)
                {
                    *argv[0] = new RValue(714);
                }
                else if (randomSecondary == InventoryHandler.SecondaryUpgradeFlags.SecondaryGarnetGem)
                {
                    *argv[0] = new RValue(710);
                }
                else if (randomSecondary == InventoryHandler.SecondaryUpgradeFlags.SecondaryRubyGem)
                {
                    *argv[0] = new RValue(706);
                }
                else if (randomSecondary == InventoryHandler.SecondaryUpgradeFlags.SecondarySapphireGem)
                {
                    *argv[0] = new RValue(702);
                }
                else if (randomSecondary == InventoryHandler.SecondaryUpgradeFlags.SecondaryOpalGem)
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
            else if (inventoryHandler.UpgradeSanity == InventoryHandler.UpgradeSetting.None)
            {
                return;
            }
            else
            {
                if (inventoryHandler.AvailableSpecialUpgrades == InventoryHandler.SpecialUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryHandler.SpecialUpgradeFlags[] availableSpecial = Enum.GetValues(typeof(InventoryHandler.SpecialUpgradeFlags)).
                    Cast<InventoryHandler.SpecialUpgradeFlags>().
                    Where(x => inventoryHandler.AvailableSpecialUpgrades.HasFlag(x) && x != InventoryHandler.SpecialUpgradeFlags.None).ToArray();

                InventoryHandler.SpecialUpgradeFlags randomSpecial = availableSpecial[rand.Next(availableSpecial.Length)];

                logger.PrintMessage(string.Join(" ", availableSpecial.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomSpecial == InventoryHandler.SpecialUpgradeFlags.SpecialEmeraldGem)
                {
                    *argv[0] = new RValue(715);
                }
                else if (randomSpecial == InventoryHandler.SpecialUpgradeFlags.SpecialGarnetGem)
                {
                    *argv[0] = new RValue(711);
                }
                else if (randomSpecial == InventoryHandler.SpecialUpgradeFlags.SpecialRubyGem)
                {
                    *argv[0] = new RValue(707);
                }
                else if (randomSpecial == InventoryHandler.SpecialUpgradeFlags.SpecialSapphireGem)
                {
                    *argv[0] = new RValue(703);
                }
                else if (randomSpecial == InventoryHandler.SpecialUpgradeFlags.SpecialOpalGem)
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
            else if (inventoryHandler.UpgradeSanity == InventoryHandler.UpgradeSetting.None)
            {
                return;
            }
            else
            {
                if (inventoryHandler.AvailableDefensiveUpgrades == InventoryHandler.DefensiveUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryHandler.DefensiveUpgradeFlags[] availableDefensive = Enum.GetValues(typeof(InventoryHandler.DefensiveUpgradeFlags)).
                    Cast<InventoryHandler.DefensiveUpgradeFlags>().
                    Where(x => inventoryHandler.AvailableDefensiveUpgrades.HasFlag(x) && x != InventoryHandler.DefensiveUpgradeFlags.None).ToArray();

                InventoryHandler.DefensiveUpgradeFlags randomDefensive = availableDefensive[rand.Next(availableDefensive.Length)];

                logger.PrintMessage(string.Join(" ", availableDefensive.Select(day => day.ToString()).ToList()), System.Drawing.Color.DarkOrange);

                if (randomDefensive == InventoryHandler.DefensiveUpgradeFlags.DefensiveEmeraldGem)
                {
                    *argv[0] = new RValue(716);
                }
                else if (randomDefensive == InventoryHandler.DefensiveUpgradeFlags.DefensiveGarnetGem)
                {
                    *argv[0] = new RValue(712);
                }
                else if (randomDefensive == InventoryHandler.DefensiveUpgradeFlags.DefensiveRubyGem)
                {
                    *argv[0] = new RValue(708);
                }
                else if (randomDefensive == InventoryHandler.DefensiveUpgradeFlags.DefensiveSapphireGem)
                {
                    *argv[0] = new RValue(704);
                }
                else if (randomDefensive == InventoryHandler.DefensiveUpgradeFlags.DefensiveOpalGem)
                {
                    *argv[0] = new RValue(700);
                }
            }

        }
    }
}