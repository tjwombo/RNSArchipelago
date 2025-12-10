using RNSReloaded.Interfaces.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RnSArchipelago.Utils
{
    internal unsafe static class ShopItemsUtil
    {

        static Random rand = new Random();

        private static long PotionNameToId(string potion)
        {
            if (potion == "Full Heal Potion")
            {
                return 487;
            } else if (potion == "Level Up Potion")
            {
                return 488;
            } else if (potion == "Regen Potion")
            {
                return 489;
            } else if (potion == "Essence of Spell")
            {
                return 490;
            } else if (potion == "Darkness Potion")
            {
                return 491;
            } else if (potion == "Quickening Potion")
            {
                return 492;
            } else if (potion == "Winged Potion")
            {
                return 493;
            } else if (potion == "Essence of Wit")
            {
                return 494;
            } else if (potion == "Swifthand Potion")
            {
                return 495;
            } else if (potion == "Fire Potion")
            {
                return 496;
            } else if (potion == "Strength Potion")
            {
                return 497;
            } else if (potion == "Gold Potion")
            {
                return 498;
            } else if (potion == "Luck Potion")
            {
                return 499;
            } else if (potion == "Essence of Steel")
            {
                return 500;
            } else if (potion == "Evasion Potion")
            {
                return 501;
            } else if (potion == "Longarm Potion")
            {
                return 502;
            } else if (potion == "Vitality Potion")
            {
                return 503;
            } else
            {
                return 0;
            }
        }

        internal static void SetHpPotion(RValue** argv, long baseArchipelagoItemId)
        {
            if (false) // TODO: If archipelago shop and we have checks per item in shop
            {
                *argv[0] = new RValue(baseArchipelagoItemId);
            } else if (InventoryUtil.Instance.PotionSanity == InventoryUtil.PotionSetting.None) {
                return;
            } else if (InventoryUtil.Instance.PotionSanity == InventoryUtil.PotionSetting.Locked) {
                if (InventoryUtil.Instance.AvailablePotions.Contains("Full Heal Potion"))
                {
                    *argv[0] = new RValue(PotionNameToId("Full Heal Potion"));
                } else
                {
                    *argv[0] = new RValue(0);
                }
            } else
            {
                string randomPotion = InventoryUtil.Instance.AvailablePotions[rand.Next(InventoryUtil.Instance.AvailablePotions.Count)];
                *argv[0] = new RValue(PotionNameToId(randomPotion));
            }
        }

        internal static void SetLevelPotion(RValue** argv, long baseArchipelagoItemId)
        {
            if (false) // TODO: If archipelago shop and we have checks per item in shop
            {
                *argv[0] = new RValue(baseArchipelagoItemId);
            }
            else if (InventoryUtil.Instance.PotionSanity == InventoryUtil.PotionSetting.None)
            {
                return;
            }
            else if (InventoryUtil.Instance.PotionSanity == InventoryUtil.PotionSetting.Locked)
            {
                if (InventoryUtil.Instance.AvailablePotions.Contains("Level Up Potion"))
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
                string randomPotion = InventoryUtil.Instance.AvailablePotions[rand.Next(InventoryUtil.Instance.AvailablePotions.Count)];
                *argv[0] = new RValue(PotionNameToId(randomPotion));
            }
        }

        internal static void SetPotion(RValue** argv, long baseArchipelagoItemId)
        {
            if (false) // TODO: If archipelago shop and we have checks per item in shop
            {
                *argv[0] = new RValue(baseArchipelagoItemId);
            }
            else if (InventoryUtil.Instance.PotionSanity == InventoryUtil.PotionSetting.None)
            {
                return;
            }
            else if (InventoryUtil.Instance.PotionSanity == InventoryUtil.PotionSetting.Locked) 
            {
                List<string> actualPotions = InventoryUtil.Instance.AvailablePotions.Where(potion => (potion != "Full Heal Potion" && potion != "Level Up Potion")).ToList();
                Console.WriteLine(String.Join(" ", actualPotions));
                string randomPotion = actualPotions[rand.Next(actualPotions.Count)];
                *argv[0] = new RValue(PotionNameToId(randomPotion));
            } else
            {
                string randomPotion = InventoryUtil.Instance.AvailablePotions[rand.Next(InventoryUtil.Instance.AvailablePotions.Count)];
                *argv[0] = new RValue(PotionNameToId(randomPotion));
            }
        }

        internal static void SetPrimaryUpgrade(RValue** argv, long baseArchipelagoItemId)
        {
            if (false) // TODO: If archipelago shop and we have checks per item in shop
            {
                *argv[0] = new RValue(baseArchipelagoItemId);
            } else if (InventoryUtil.Instance.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {

                if (InventoryUtil.Instance.AvailablePrimaryUpgrades == InventoryUtil.PrimaryUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.PrimaryUpgradeFlags[] availablePrimary = Enum.GetValues(typeof(InventoryUtil.PrimaryUpgradeFlags)).
                    Cast<InventoryUtil.PrimaryUpgradeFlags>().
                    Where(x => InventoryUtil.Instance.AvailablePrimaryUpgrades.HasFlag(x) && x != InventoryUtil.PrimaryUpgradeFlags.None).ToArray();

                InventoryUtil.PrimaryUpgradeFlags randomPrimary = availablePrimary[rand.Next(availablePrimary.Length)];

                Console.WriteLine(String.Join(" ", availablePrimary.Select(day => day.ToString()).ToList()));

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

        internal static void SetSecondaryUpgrade(RValue** argv, long baseArchipelagoItemId)
        {
            if (false) // TODO: If archipelago shop and we have checks per item in shop
            {
                *argv[0] = new RValue(baseArchipelagoItemId);
            } else if (InventoryUtil.Instance.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {
                if (InventoryUtil.Instance.AvailableSecondaryUpgrades == InventoryUtil.SecondaryUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.SecondaryUpgradeFlags[] availableSecondary = Enum.GetValues(typeof(InventoryUtil.SecondaryUpgradeFlags)).
                    Cast<InventoryUtil.SecondaryUpgradeFlags>().
                    Where(x => InventoryUtil.Instance.AvailableSecondaryUpgrades.HasFlag(x) && x != InventoryUtil.SecondaryUpgradeFlags.None).ToArray();

                InventoryUtil.SecondaryUpgradeFlags randomSecondary = availableSecondary[rand.Next(availableSecondary.Length)];

                Console.WriteLine(String.Join(" ", availableSecondary.Select(day => day.ToString()).ToList()));

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

        internal static void SetSpecialUpgrade(RValue** argv, long baseArchipelagoItemId)
        {
            if (false) // TODO: If archipelago shop and we have checks per item in shop
            {
                *argv[0] = new RValue(baseArchipelagoItemId);
            } else if (InventoryUtil.Instance.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {
                if (InventoryUtil.Instance.AvailableSpecialUpgrades == InventoryUtil.SpecialUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.SpecialUpgradeFlags[] availableSpecial = Enum.GetValues(typeof(InventoryUtil.SpecialUpgradeFlags)).
                    Cast<InventoryUtil.SpecialUpgradeFlags>().
                    Where(x => InventoryUtil.Instance.AvailableSpecialUpgrades.HasFlag(x) && x != InventoryUtil.SpecialUpgradeFlags.None).ToArray();

                InventoryUtil.SpecialUpgradeFlags randomSpecial = availableSpecial[rand.Next(availableSpecial.Length)];

                Console.WriteLine(String.Join(" ", availableSpecial.Select(day => day.ToString()).ToList()));

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

        internal static void SetDefensiveUpgrade(RValue** argv, long baseArchipelagoItemId)
        {
            if (false) // TODO: If archipelago shop and we have checks per item in shop
            {
                *argv[0] = new RValue(baseArchipelagoItemId);
            } else if (InventoryUtil.Instance.UpgradeSanity == InventoryUtil.UpgradeSetting.None) {
                return;
            } else {
                if (InventoryUtil.Instance.AvailableDefensiveUpgrades == InventoryUtil.DefensiveUpgradeFlags.None)
                {
                    *argv[0] = new RValue(0);
                    return;
                }

                InventoryUtil.DefensiveUpgradeFlags[] availableDefensive = Enum.GetValues(typeof(InventoryUtil.DefensiveUpgradeFlags)).
                    Cast<InventoryUtil.DefensiveUpgradeFlags>().
                    Where(x => InventoryUtil.Instance.AvailableDefensiveUpgrades.HasFlag(x) && x != InventoryUtil.DefensiveUpgradeFlags.None).ToArray();

                InventoryUtil.DefensiveUpgradeFlags randomDefensive = availableDefensive[rand.Next(availableDefensive.Length)];

                Console.WriteLine(String.Join(" ", availableDefensive.Select(day => day.ToString()).ToList()));

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
