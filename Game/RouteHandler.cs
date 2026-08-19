using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using RnSArchipelago.Connection;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class RouteHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private readonly LocationHandler locationHandler;
        private readonly ArchipelagoConnection conn;

        internal IHook<ScriptDelegate>? chooseHallsHook;
        internal IHook<ScriptDelegate>? endHallsHook;
        internal IHook<ScriptDelegate>? characterChosenHook;

        internal string lastVisitedRunType = "";

        internal RouteHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, InventoryHandler inventoryHandler, LocationHandler locationHandler, ArchipelagoConnection conn)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.locationHandler = locationHandler;
            this.conn = conn;

            this.inventoryHandler.UpdateHallwayOnItemRecieve += OnKingdomRecieve;
        }

        // Update the kingdoms we visit and the number of kingdoms we should be visiting
        internal void OnKingdomRecieve(bool currentHallwayPosAware = true)
        {
            UpdateRoute(currentHallwayPosAware);

            UpdateRouteLength();
        }

        // End the route early if we arent allowed to continue
        private bool EndRouteEarly()
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {

                HookUtil.FindElementInLayer("RunMenu_Blocker", "hallkey", out var element);

                if (element == null)
                {
                    return false;
                }

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                var hallkey = instanceValue.Get("hallkey");

                var kingdoms = KingdomUtil.GetRunnableKingdoms(lastVisitedRunType);
                var maxVisitableKingdoms = kingdoms.Count;
                if (kingdoms.Contains("hw_outskirts"))
                {
                    maxVisitableKingdoms--;
                }
                if (kingdoms.Contains("hw_geode"))
                {
                    maxVisitableKingdoms--;
                }

                if (this.inventoryHandler.isProgressive)
                {
                    maxVisitableKingdoms = (int)Math.Min(maxVisitableKingdoms, this.inventoryHandler.ProgressiveRegions);
                }

                maxVisitableKingdoms = (int)Math.Min(maxVisitableKingdoms, this.inventoryHandler.maxKingdoms);

                var hallwayNumber = HookUtil.GetNumeric(rnsReloaded.utils.GetGlobalVar("hallwayCurrent")); // 0 is Kingdom Outskirts / Crack in the Geode

                if (hallwayNumber < maxVisitableKingdoms)
                {
                    return false;
                }

                var currentPos = instanceValue.Get("currentPos");
                var notchNumber = instanceValue.Get("notchNumber");

                if (currentPos != null && notchNumber != null)
                {
                    // Check to see if we are at the last notch in the hallway
                    if (HookUtil.IsEqualToNumeric(currentPos, HookUtil.GetNumeric(notchNumber) - 1))
                    {
                        if (hallwayNumber == maxVisitableKingdoms)
                        {
                            return rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 1)) != "hw_keep" && rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 1)) != "hw_darkhall";
                        }
                        else if (hallwayNumber == maxVisitableKingdoms + 1)
                        {
                            return rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 2)) != "hw_pinnacle" && rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 2)) != "hw_reflection";
                        }
                    }
                }

            }
            return false;
        }

        // Update the route length to the maximum value value
        private void UpdateRouteLength()
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "hallkey", out var element);

                if (element == null)
                {
                    return;
                }

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                var kingdoms = KingdomUtil.GetRunnableKingdoms(lastVisitedRunType);
                var maxCanRun = kingdoms.Count;
                if (kingdoms.Contains("hw_outskirts"))
                {
                    maxCanRun--;
                }
                if (kingdoms.Contains("hw_geode"))
                {
                    maxCanRun--;
                }
                maxCanRun = (int)Math.Min(maxCanRun, this.inventoryHandler.maxKingdoms);

                if (this.inventoryHandler.isProgressive)
                {
                    maxCanRun = (int)Math.Min(maxCanRun, this.inventoryHandler.ProgressiveRegions);
                }
            }
        }

        // Ends the route early if kingdom sanity is enabled, but not enough kingdoms are unlocked, or progressive kingdom count != maxKingdoms
        internal RValue* ManageRouteLength(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryHandler.isActive)
                {
                    // Backup send location in case they disconnected during the fight
                    HookUtil.FindElementInLayer("RunMenu_Blocker", "currentPos", out var element);
                    if (element != null)
                    {
                        var instance = new RValue(((CLayerInstanceElement*)element)->Instance);
                        var currentPos = HookUtil.GetNumeric(instance.Get("currentPos")); // Wasn't playing well with IsEqualToNumeric
                        var hallwayPos = HookUtil.GetNumeric(instance.Get("hallwayPos"));
                        var index = HookUtil.GetNumeric(instance.Get("currentPos"));

                        // First check is to prevent shop, second is general transitions, and last is final boss
                        if ((currentPos != 0 || hallwayPos != 0) && index != -1 && (hallwayPos != this.inventoryHandler.maxKingdoms + 2 && currentPos == 0))
                        {
                            locationHandler.SendNotchLoctaion();
                        }
                    }

                    if (EndRouteEarly())
                    {
                        rnsReloaded.ExecuteScript("scr_hallwayprogress_make_defeat", self, other, []);

                        return returnValue;
                    }
                }
            }
            if (this.endHallsHook != null)
            {
                returnValue = this.endHallsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call end halls hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Modify the route to take a route that corresponds to the kingdom order
        internal void ModifyRoute(int maxCanRun, InventoryHandler.KingdomFlags visitableKingdoms, bool currentHallwayPosAware)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "stageNameKey", out var element);

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                var unplacedKingdoms = KingdomUtil.GetRunnableKingdoms(lastVisitedRunType);

                var hallkey = instanceValue.Get("hallkey");
                var maxKingdoms = this.inventoryHandler.maxKingdoms;

                var currentHallwayPos = (int)HookUtil.GetNumeric(instanceValue.Get("hallwayPos"));
                var currentPos = (int)HookUtil.GetNumeric(instanceValue.Get("currentPos"));

                // Handle the 0th position
                if (!currentHallwayPosAware || currentHallwayPos < 0 || (currentPos <= 0 && currentHallwayPos == 0))
                {
                    if (unplacedKingdoms.Contains("hw_outskirts") && unplacedKingdoms.Contains("hw_geode"))
                    {
                        if (lastVisitedRunType == "kingdom")
                        {
                            KingdomUtil.SetHallwayValue(0, instanceValue, "hw_outskirts", 7);
                        }
                        else
                        {
                            KingdomUtil.SetHallwayValue(0, instanceValue, "hw_geode", 9);
                        }
                    }
                    else if (unplacedKingdoms.Contains("hw_outskirts"))
                    {
                        KingdomUtil.SetHallwayValue(0, instanceValue, "hw_outskirts", 7);
                    }
                    else if (unplacedKingdoms.Contains("hw_geode"))
                    {
                        KingdomUtil.SetHallwayValue(0, instanceValue, "hw_geode", 9);
                    }

                    // Generate the hallway data
                    if (rnsReloaded.ArrayGetEntry(hallkey, 0)->ToString().Equals("hw_outskirts"))
                    {
                        rnsReloaded.ExecuteScript("scr_hallwaygen_outskirts", instance->Instance, null, []);
                    }
                    else if (rnsReloaded.ArrayGetEntry(hallkey, 0)->ToString().Equals("hw_geode"))
                    {
                        rnsReloaded.ExecuteScript("scr_hallwaygen_geode", instance->Instance, null, []);
                    }

                    // Update the name
                    *instanceValue.Get("stageNameRefresh") = new RValue(1);
                }

                unplacedKingdoms.Remove("hw_outskirts");
                unplacedKingdoms.Remove("hw_geode");

                if (maxCanRun == 0)
                {
                    KingdomUtil.SetHallwayValue(1, instanceValue, "", 0);
                    return;
                }

                // Handle the 1st position, trying to encorporate their request
                if (!unplacedKingdoms.Contains(rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, 1))))
                {
                    int selectedIndex = KingdomUtil.GetWeightedKingdom(conn, unplacedKingdoms);
                    KingdomUtil.SetHallwayValue(1, instanceValue, unplacedKingdoms[selectedIndex], 6);
                    unplacedKingdoms.Remove(unplacedKingdoms[selectedIndex]);
                }
                else
                {
                    string originalKingdom = rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, 1));
                    KingdomUtil.SetHallwayValue(1, instanceValue, originalKingdom, 6);
                    unplacedKingdoms.Remove(originalKingdom);
                }

                // Perform initial limiting
                if (currentHallwayPosAware)
                {
                    // Remove kingdoms that are already placed for the list of possible kingdoms
                    for (var i = 2; i <= currentHallwayPos; i++)
                    {
                        string placedKingdom = rnsReloaded.ArrayGetEntry(hallkey, i)->ToString();
                        KingdomUtil.SetHallwayValue(i, instanceValue, placedKingdom, 6);
                        unplacedKingdoms.Remove(placedKingdom);
                    }

                    // We've already handled pos 0 and 1, so we need to start at least at 2
                    currentHallwayPos = Math.Max(currentHallwayPos + 1, 2);
                }

                // Assign the remaining kingdoms
                for (var i = currentHallwayPosAware ? currentHallwayPos : 2; i <= maxCanRun; i++)
                {
                    var availibleNthKingdoms = KingdomUtil.GetOrderedRunnableKingdoms(lastVisitedRunType, i).Intersect(unplacedKingdoms).ToList();

                    // Prioritize the kingdom of the correct order
                    if (availibleNthKingdoms.Count != 0)
                    {
                        int selectedIndex = KingdomUtil.GetWeightedKingdom(conn, availibleNthKingdoms);
                        KingdomUtil.SetHallwayValue(i, instanceValue, availibleNthKingdoms[selectedIndex], 6);
                        unplacedKingdoms.Remove(availibleNthKingdoms[selectedIndex]);
                    }
                    else
                    {
                        int selectedIndex = KingdomUtil.GetWeightedKingdom(conn, unplacedKingdoms);
                        KingdomUtil.SetHallwayValue(i, instanceValue, unplacedKingdoms[selectedIndex], 6);
                        unplacedKingdoms.Remove(unplacedKingdoms[selectedIndex]);
                    }
                }

                // Place the last 2 where they need to be, if they are visitable 
                var isProgressive = this.inventoryHandler.isProgressive;
                if (maxCanRun == maxKingdoms && (!isProgressive || this.inventoryHandler.ProgressiveRegions >= maxKingdoms + 1))
                {
                    if (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Kingdom || (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Either && lastVisitedRunType == "kingdom"))
                    {
                        if ((visitableKingdoms & InventoryHandler.KingdomFlags.The_Pale_Keep) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "hw_keep", 0);
                        }
                        else
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "", 0);
                        }
                    }
                    else if (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Extra || (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Either && lastVisitedRunType == "extra"))
                    {
                        if ((visitableKingdoms & InventoryHandler.KingdomFlags.Looping_Hallway) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "hw_darkhall", 13);
                        }
                        else
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "", 0);
                        }
                    }
                    else if (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Combined)
                    {
                        if ((visitableKingdoms & InventoryHandler.KingdomFlags.The_Pale_Keep) != 0 && (visitableKingdoms & InventoryHandler.KingdomFlags.Looping_Hallway) != 0)
                        {
                            List<string> kingdoms = ["hw_keep", "hw_darkhall"];
                            int selectedIndex = KingdomUtil.GetWeightedKingdom(conn, kingdoms);
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, kingdoms[selectedIndex], selectedIndex == 0 ? 0 : 13);
                        }
                        else if ((visitableKingdoms & InventoryHandler.KingdomFlags.The_Pale_Keep) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "hw_keep", 0);
                        }
                        else if ((visitableKingdoms & InventoryHandler.KingdomFlags.Looping_Hallway) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "hw_darkhall", 13);
                        }
                        else
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "", 0);
                        }
                    }
                }
                else
                {
                    KingdomUtil.SetHallwayValue(maxCanRun + 1, instanceValue, "", 0);
                }

                if (maxCanRun == maxKingdoms && (!isProgressive || this.inventoryHandler.ProgressiveRegions >= maxKingdoms + 2))
                {
                    if (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Kingdom || (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Either && lastVisitedRunType == "kingdom"))
                    {
                        if ((visitableKingdoms & InventoryHandler.KingdomFlags.Moonlit_Pinnacle) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "hw_pinnacle", 0);
                        }
                        else
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "", 0);
                        }
                    }
                    else if (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Extra || (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Either && lastVisitedRunType == "extra"))
                    {
                        if ((visitableKingdoms & InventoryHandler.KingdomFlags.Reflecting_Pool) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "hw_reflection", 0);
                        }
                        else
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "", 0);
                        }
                    }
                    else if (this.inventoryHandler.run_type == InventoryHandler.RunTypeSetting.Combined)
                    {
                        if ((visitableKingdoms & InventoryHandler.KingdomFlags.Moonlit_Pinnacle) != 0 && (visitableKingdoms & InventoryHandler.KingdomFlags.Reflecting_Pool) != 0)
                        {
                            List<string> kingdoms = ["hw_pinnacle", "hw_reflection"];
                            int selectedIndex = KingdomUtil.GetWeightedKingdom(conn, kingdoms);
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, kingdoms[selectedIndex], selectedIndex == 0 ? 0 : 13);
                        }
                        else if ((visitableKingdoms & InventoryHandler.KingdomFlags.Moonlit_Pinnacle) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "hw_pinnacle", 0);
                        }
                        else if ((visitableKingdoms & InventoryHandler.KingdomFlags.Reflecting_Pool) != 0)
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "hw_reflection", 13);
                        }
                        else
                        {
                            KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "", 0);
                        }
                    }
                }
                else
                {
                    KingdomUtil.SetHallwayValue(maxCanRun + 2, instanceValue, "", 0);
                }
            }

        }

        // Update the route from the start or from the current position + 1
        internal void UpdateRoute(bool currentHallwayPosAware)
        {
            this.logger.PrintMessage("Updating route", System.Drawing.Color.DarkOrange);

            var visitableKingdoms = this.inventoryHandler.AvailableKingdoms;

            var kingdoms = KingdomUtil.GetRunnableKingdoms(lastVisitedRunType);
            var maxCanRun = kingdoms.Count;
            if (kingdoms.Contains("hw_outskirts"))
            {
                maxCanRun--;
            }
            if (kingdoms.Contains("hw_geode"))
            {
                maxCanRun--;
            }
            maxCanRun = (int)Math.Min(maxCanRun, this.inventoryHandler.maxKingdoms);

            if (this.inventoryHandler.isProgressive)
            {
                maxCanRun = (int)Math.Min(maxCanRun, this.inventoryHandler.ProgressiveRegions);
            }

            this.logger.PrintMessage("Route length: " + maxCanRun, System.Drawing.Color.DarkOrange);

            ModifyRoute(maxCanRun, visitableKingdoms, currentHallwayPosAware);
        }

        // Create the route such that you only visit kingdoms you are allowed to with your settings and items combo
        internal RValue* CreateRoute(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryHandler.isActive)
                {
                    var isKingdomSanity = this.inventoryHandler.isKingdomSanity;
                    var isProgressive = this.inventoryHandler.isProgressive;
                    if (isKingdomSanity || isProgressive)
                    {
                        if (this.chooseHallsHook != null)
                        {
                            returnValue = this.chooseHallsHook.OriginalFunction(self, other, returnValue, argc, argv);
                        }
                        else
                        {
                            this.logger.PrintMessage("Unable to call choose halls hook", System.Drawing.Color.Red);
                        }

                        OnKingdomRecieve(false);

                        return returnValue;
                    }
                }
            }
            if (this.chooseHallsHook != null)
            {
                returnValue = this.chooseHallsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call choose halls hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Updates the route once you select a character
        internal RValue* UpdateRouteOnCharacterSelect(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.characterChosenHook != null)
            {
                returnValue = this.characterChosenHook.OriginalFunction(self, other, returnValue, argc, argv);

                UpdateRoute(false);
            }
            else
            {
                this.logger.PrintMessage("Unable to call choose halls hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }
    }
}
