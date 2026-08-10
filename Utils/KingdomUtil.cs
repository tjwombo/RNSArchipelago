using RnSArchipelago.Connection;
using RnSArchipelago.Game;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RnSArchipelago.Utils
{
    internal static class KingdomUtil
    {
        internal static WeakReference<IRNSReloaded> rnsReloadedRef = null!;
        internal static InventoryHandler inventoryHandler = null!;

        // Gets the kingdoms you can visit for your run, excluding the ending hallways
        internal static List<string> GetRunnableKingdoms(string lastVisitedRunType)
        {
            if (inventoryHandler.RunType == InventoryHandler.RunTypeSetting.Combined)
            {
                return inventoryHandler.GetChaosKingdomsAvailable();
            }
            else if (inventoryHandler.RunType == InventoryHandler.RunTypeSetting.Kingdom)
            {
                return inventoryHandler.GetKingdomKingdomsAvailable();
            }
            else if (inventoryHandler.RunType == InventoryHandler.RunTypeSetting.Extra)
            {
                return inventoryHandler.GetExtraKingdomsAvailable();
            }
            else if (inventoryHandler.RunType == InventoryHandler.RunTypeSetting.Either)
            {
                List<string> kingdoms;

                // Try our tab and if nothing is found go to the other tab
                if (lastVisitedRunType == "kingdom")
                {
                    kingdoms = inventoryHandler.GetKingdomKingdomsAvailable();
                    if (kingdoms.Count > 0)
                    {
                        lastVisitedRunType = "kingdom";
                        return kingdoms;
                    }
                    kingdoms = inventoryHandler.GetExtraKingdomsAvailable();
                    lastVisitedRunType = "extra";
                    return kingdoms;
                }
                else if (lastVisitedRunType == "extra")
                {
                    kingdoms = inventoryHandler.GetExtraKingdomsAvailable();
                    if (kingdoms.Count > 0)
                    {
                        lastVisitedRunType = "extra";
                        return kingdoms;
                    }

                    kingdoms = inventoryHandler.GetKingdomKingdomsAvailable();
                    lastVisitedRunType = "kingdom";
                    return kingdoms;
                }
                // Otherwise default to assuming it was a kingdom tab
                else
                {
                    kingdoms = inventoryHandler.GetKingdomKingdomsAvailable();
                    if (kingdoms.Count > 0)
                    {
                        lastVisitedRunType = "kingdom";
                        return kingdoms;
                    }

                    kingdoms = inventoryHandler.GetExtraKingdomsAvailable();
                    lastVisitedRunType = "extra";
                    return kingdoms;
                }
            }

            return [];
        }

        // Gets the kingdoms you can visit for your run at a given kingdom order, excluding the ending hallways
        internal static List<string> GetOrderedRunnableKingdoms(string lastVisitedRunType, int n)
        {
            if (inventoryHandler.RunType == InventoryHandler.RunTypeSetting.Combined)
            {
                return inventoryHandler.GetChaosKingdomsAvailable(n);
            }
            else if (inventoryHandler.RunType == InventoryHandler.RunTypeSetting.Kingdom || (inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Extra && lastVisitedRunType == "kingdom"))
            {
                return inventoryHandler.GetKingdomKingdomsAvailable(n);
            }
            else if (inventoryHandler.RunType == InventoryHandler.RunTypeSetting.Extra || (inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Extra && lastVisitedRunType == "extra"))
            {
                return inventoryHandler.GetExtraKingdomsAvailable(n);
            }

            return [];
        }

        private static readonly string[] locationSuffix = [" Battle 1", " Battle 2", " Battle 3", " Chest", " Boss"];
        private static readonly int baseLocationWeight = 1;
        private static readonly int finalBossLocationWeight = 5;
        private static readonly int chestLocationWeight = 1;
        private static readonly int shopLocationWeight = 1;
        private static readonly int classLocationModifier = 3;

        // Return the index of the kingdom that is chosen weighted randomly prioritizing kingdoms with more checks remaining
        internal static int GetWeightedKingdom(ArchipelagoConnection conn, List<string> kingdoms)
        {
            if (conn.session != null)
            {
                var locations = conn.session.Locations.AllMissingLocations;
                var character = HookUtil.GetClass();

                var weights = new int[kingdoms.Count];
                double sum = 0;

                // Assign weights for each kingdom
                for (var i = 0; i < kingdoms.Count; i++)
                {
                    // Each kingdom always has a chance to appear
                    weights[i] = 1;
                    sum += 1;

                    var kingdom = InventoryHandler.KingdomNotchToLocationName(kingdoms[i]);

                    // Add weights for each of the standard locations
                    for (var j = 0; j < locationSuffix.Length; j++)
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + locationSuffix[j])))
                        {
                            weights[i] += baseLocationWeight;
                            sum += baseLocationWeight;
                        }

                        if (character != "")
                        {
                            if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + locationSuffix[j] + " - " + character)))
                            {
                                weights[i] += baseLocationWeight * classLocationModifier;
                                sum += baseLocationWeight * classLocationModifier;
                            }
                        }
                    }

                    // Chest for chest item positions
                    for (var j = 0; j < LocationHandler.CHEST_POSITIONS.Length; j++)
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + " Chest " + LocationHandler.CHEST_POSITIONS[j])))
                        {
                            weights[i] += chestLocationWeight;
                            sum += chestLocationWeight;
                        }
                    }

                    // Check for regional shop item positions
                    for (var j = 0; j < LocationHandler.SHOP_POSITIONS.Length; j++)
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + " Chest " + LocationHandler.SHOP_POSITIONS[j])))
                        {
                            weights[i] += shopLocationWeight;
                            sum += shopLocationWeight;
                        }
                    }

                    // Check for Shira/Witch
                    if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom)))
                    {
                        weights[i] += finalBossLocationWeight;
                        sum += finalBossLocationWeight;
                    }

                    if (character != "")
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + " - " + character)))
                        {
                            weights[i] += finalBossLocationWeight * classLocationModifier;
                            sum += finalBossLocationWeight * classLocationModifier;
                        }
                    }
                }

                var rand = new Random();
                double value = rand.NextDouble();

                for (var i = 0; i < kingdoms.Count; i++)
                {
                    if (weights.Take(i + 1).Sum() / sum >= value)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        internal static unsafe void SetHallwayValue(int index, RValue* hallkey, string hallValue, RValue* hallsubimg, int hallsubimgValue)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                // Add to the hallkey, if we are unable to place it
                var hallkeyLength = rnsReloaded.ArrayGetLength(hallkey);
                if (hallkeyLength.HasValue && HookUtil.GetNumeric(hallkeyLength.Value) < index + 1)
                {
                    var endArray = new RValue[(index + 1) - HookUtil.GetNumeric(hallkeyLength.Value) + 1]; // first +1 is to turn 0-based index into length, second is to account for array_push
                    endArray[0] = *hallkey;
                    rnsReloaded.ExecuteCodeFunction("array_push", null, null, endArray);
                }

                // Add to the hallkey, if we are unable to place it
                var hallsubimgLength = rnsReloaded.ArrayGetLength(hallsubimg);
                if (hallsubimgLength.HasValue && HookUtil.GetNumeric(hallsubimgLength.Value) < index + 1)
                {
                    var endArray = new RValue[(index + 1) - HookUtil.GetNumeric(hallsubimgLength.Value) + 1];
                    endArray[0] = *hallsubimg;
                    rnsReloaded.ExecuteCodeFunction("array_push", null, null, endArray);
                }

                rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, index), hallValue);
                *rnsReloaded.ArrayGetEntry(hallsubimg, index) = new(hallsubimgValue);
            }
        }
    }
}
