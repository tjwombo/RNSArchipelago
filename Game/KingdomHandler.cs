using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Diagnostics.CodeAnalysis;
using static RnSArchipelago.Utils.HookUtil;

namespace RnSArchipelago.Game
{
    internal unsafe class KingdomHandler
    {
        private readonly WeakReference<IRNSReloaded>? rnsReloadedRef;
        private readonly ILoggerV1 logger;
        internal Config.Config? modConfig;

        internal IHook<ScriptDelegate>? chooseHallsHook;
        internal IHook<ScriptDelegate>? endHallsHook;
        internal IHook<ScriptDelegate>? fixChooseIconsHook;
        internal IHook<ScriptDelegate>? fixEndIconsHook;

        private bool IsReady(
            [MaybeNullWhen(false), NotNullWhen(true)] out IRNSReloaded rnsReloaded
        )
        {
            if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out rnsReloaded))
            {
                return rnsReloaded != null;
            }
            this.logger.PrintMessage("Unable to find rnsReloaded in KingdomHandler", System.Drawing.Color.Red);
            rnsReloaded = null;
            return false;
        }

        internal KingdomHandler(WeakReference<IRNSReloaded>? rnsReloadedRef, ILoggerV1 logger, Config.Config modConfig)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.modConfig = modConfig;
        }

        // Calcuate the number of kingdoms your currently allowed to visit in a run
        private static int CalculateMaxRun()
        {
            var isProgressive = InventoryUtil.Instance.isProgressive;
            var maxKingdoms = InventoryUtil.Instance.maxKingdoms;
            var regionCount = InventoryUtil.Instance.ProgressiveRegions;
            var visitableKingdoms = InventoryUtil.Instance.AvailableKingdoms;

            var maxOrder = maxKingdoms;
            if (isProgressive)
            {
                maxOrder = Math.Min(maxOrder, regionCount);
            }

            var maxCanRun = 0;
            var unaccountedVisitableKingdoms = new List<string>();
            var accountedForKingdoms = new List<string>();
            for (var i = 1; i <= maxOrder; i++)
            {
                var reachableKingdomsByOrder = InventoryUtil.Instance.GetNthOrderKingdoms(i);
                var runBefore = maxCanRun;
                if (reachableKingdomsByOrder.Count == 0)
                {
                    foreach (var kingdom in unaccountedVisitableKingdoms)
                    {
                        if (accountedForKingdoms.Contains(kingdom))
                        {
                            continue;
                        }
                        switch (kingdom)
                        {
                            case "hw_nest":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0) {
                                    accountedForKingdoms.Add("hw_nest");
                                    maxCanRun++;
                                }
                                break;
                            case "hw_arsenal":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                                {
                                   accountedForKingdoms.Add("hw_arsenal");
                                   maxCanRun++;
                                }
                                break;
                            case "hw_lakeside":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                                {
                                    accountedForKingdoms.Add("hw_lakeside");
                                    maxCanRun++;
                                }
                                break;
                            case "hw_streets":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                                {
                                    accountedForKingdoms.Add("hw_streets");
                                    maxCanRun++;
                                }
                                break;
                            case "hw_lighthouse":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                                {
                                    accountedForKingdoms.Add("hw_lighthouse");
                                    maxCanRun++;
                                }
                                break;
                        }
                        if (runBefore != maxCanRun)
                        {
                            unaccountedVisitableKingdoms.Remove(kingdom);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var kingdom in reachableKingdomsByOrder)
                    {
                        if (accountedForKingdoms.Contains(kingdom))
                        {
                            continue;
                        }
                        switch (kingdom)
                        {
                            case "hw_nest":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                                {
                                    accountedForKingdoms.Add("hw_nest");
                                    maxCanRun++;
                                }
                                break;
                            case "hw_arsenal":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                                {
                                    accountedForKingdoms.Add("hw_arsenal");
                                    maxCanRun++;
                                }
                                break;
                            case "hw_lakeside":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                                {
                                    accountedForKingdoms.Add("hw_lakeside");
                                    maxCanRun++;
                                }
                                break;
                            case "hw_streets":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                                {
                                    accountedForKingdoms.Add("hw_streets");
                                    maxCanRun++;
                                }
                                break;
                            case "hw_lighthouse":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                                {
                                    accountedForKingdoms.Add("hw_lighthouse");
                                    maxCanRun++;
                                }
                                break;
                        }
                        if (runBefore != maxCanRun)
                        {
                            reachableKingdomsByOrder.Remove(kingdom);
                            unaccountedVisitableKingdoms = [.. unaccountedVisitableKingdoms, .. reachableKingdomsByOrder];
                            break;
                        }
                    }
                }
                if (runBefore == maxCanRun)
                {
                    break;
                }
            }
            return maxCanRun;
        }

        // TODO: CANT SEEM TO ACTUALLY MODIFY THE END SCREEN KINGDOM POSITIONS
        internal RValue* ModifyEndScreenIcons(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Modify End Screen Icons", System.Drawing.Color.DarkOrange);
                    }
                    var a = new RValue(self);
                    //this.logger.PrintMessage(rnsReloaded.GetString(&a), System.Drawing.Color.DarkOrange);

                    //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "end", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);
                    //this.fixEndIconsHook.Disable();
                    HookUtil.FindLayer("RunMenu_Squares", out var layer);
                    //this.logger.PrintMessage(layer->Elements.Count + "", System.Drawing.Color.DarkOrange);

                    CLayerElementBase* hallway = layer->Elements.First;
                    if (layer != null)
                    {
                        //this.logger.PrintMessage("not null: " + layer->Elements.Count, System.Drawing.Color.DarkOrange);
                        //var a = new RValue(self);
                        //this.logger.PrintMessage(rnsReloaded.GetString(&a), System.Drawing.Color.DarkOrange);
                        hallway = layer->Elements.First;
                        while (hallway != null)
                        {
                            var instance = (CLayerInstanceElement*)hallway;
                            var instanceValue = new RValue(instance->Instance);


                            var seed = rnsReloaded.FindValue((&instanceValue)->Object, "potY");
                            if (seed != null && seed->ToString() != "unset")
                            {
                                //this.logger.PrintMessage(rnsReloaded.GetString(seed) + "", System.Drawing.Color.RebeccaPurple);
                                //ModifyElementVariable(rnsReloaded, hallway, "potY", ModificationType.ModifyArray, [new(0), new(400)]);
                                //this.logger.PrintMessage(rnsReloaded.GetString(seed) + "", System.Drawing.Color.RebeccaPurple);
                                //var b = new RValue(self);
                                //this.logger.PrintMessage(rnsReloaded.GetString(&b), System.Drawing.Color.DarkOrange);
                                if (modConfig?.ExtraDebugMessages ?? false)
                                {
                                    this.logger.PrintMessage("Before Original Function End Screen", System.Drawing.Color.DarkOrange);
                                }
                                if (this.fixEndIconsHook != null)
                                {
                                    returnValue = this.fixEndIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                                } else
                                {
                                    this.logger.PrintMessage("Unable to call fix end icons hook", System.Drawing.Color.Red);
                                }
                                if (modConfig?.ExtraDebugMessages ?? false)
                                {
                                    this.logger.PrintMessage("Before Return End Screen", System.Drawing.Color.DarkOrange);
                                }
                                return returnValue;
                            }
                            //break;
                            //}
                            hallway = hallway->Next;
                        }
                    }
                }
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function End Screen", System.Drawing.Color.DarkOrange);
            }
            if (this.fixEndIconsHook != null)
            {
                returnValue = this.fixEndIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call fix end icons hook", System.Drawing.Color.Red);
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return End Screen", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // End the route early if we arent allowed to continue
        private bool EndRouteEarly()
        {
            if (IsReady(out var rnsReloaded))
            {
                var maxVisitableKingdoms = CalculateMaxRun();
                var hallwayNumber = HookUtil.GetNumeric(rnsReloaded.utils.GetGlobalVar("hallwayCurrent")); // 0 is Kingdom Outskirts

                if (hallwayNumber < maxVisitableKingdoms)
                {
                    return false;
                }

                FindLayer("RunMenu_Blocker", out var layer);
                if (layer != null)
                {
                    var hallway = layer->Elements.First;
                    while (hallway != null)
                    {
                        var instance = (CLayerInstanceElement*)hallway;
                        var instanceValue = new RValue(instance->Instance);

                        var currentPos = instanceValue.Get("currentPos");
                        var notchNumber = instanceValue.Get("notchNumber");

                        if (currentPos != null && notchNumber != null)
                        {
                            // Check to see if we are at the last notch in the hallway
                            if (HookUtil.IsEqualToNumeric(currentPos, HookUtil.GetNumeric(notchNumber) - 1))
                            {
                                var hallkey = instanceValue.Get("hallkey");
                                if (hallwayNumber == maxVisitableKingdoms)
                                {
                                    return rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 1)) != "hw_keep";
                                }
                                else if (hallwayNumber == maxVisitableKingdoms + 1)
                                {
                                    return rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 2)) != "hw_pinnacle";
                                }
                            }
                        }
                        hallway = hallway->Next;
                    }
                }
            }
            return false;
        }

        // Update the route length to the maximum value value
        private void UpdateRouteLength()
        {
            if (IsReady(out var rnsReloaded))
            {
                FindLayer("RunMenu_Blocker", out var layer);
                if (layer != null)
                {
                    var hallway = layer->Elements.First;
                    while (hallway != null)
                    {
                        var instance = (CLayerInstanceElement*)hallway;
                        var instanceValue = new RValue(instance->Instance);
                        var hallkey = instanceValue.Get("hallkey");
                        var maxCanRun = CalculateMaxRun();
                        
                        if (hallkey != null && hallkey->ToString() != "unset" && HookUtil.GetNumeric(instanceValue.Get("hallwayNumber")) != maxCanRun + 3)
                        {
                            // Always add 3, so that we dont get the weird Shira visual glitch and account for outskirts
                            HookUtil.ModifyElementVariable(hallway, "hallwayNumber", ModificationType.ModifyLiteral, [new(maxCanRun + 3)]);
                            return;
                        }
                        hallway = hallway->Next;
                    }
                }
            }
        }

        // Ends the route early if kingdom sanity is enabled, but not enough kingdoms are unlocked, or progressive kingdom count != maxKingdoms
        internal RValue* ManageRouteLength(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Manage Route Length", System.Drawing.Color.DarkOrange);
                    }

                    if (InventoryUtil.Instance.shouldUpdateKingdomRoute)
                    {
                        InventoryUtil.Instance.shouldUpdateKingdomRoute = false;
                        UpdateRoute();

                        UpdateRouteLength();
                    }

                    if (EndRouteEarly())
                    {
                        rnsReloaded.ExecuteScript("scr_hallwayprogress_make_defeat", self, other, []);
                        if (modConfig?.ExtraDebugMessages ?? false)
                        {
                            this.logger.PrintMessage("Before Return Manage Route End", System.Drawing.Color.DarkOrange);
                        }
                        return returnValue;
                    }
                } else if (HookUtil.IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                {
                    return returnValue;
                }
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Manage Route Length", System.Drawing.Color.DarkOrange);
            }
            if (this.endHallsHook != null)
            {
                returnValue = this.endHallsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call end halls hook", System.Drawing.Color.Red);
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Manage Route Length", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Modify the hallseed and hallway icons for extra visitable kingdoms
        private void ModifyHallSeedAndIcons(int maxCanRun)
        {
            if (IsReady(out var rnsReloaded))
            {
                HookUtil.FindLayer("RunMenu_Blocker", out var layer);
                if (layer != null)
                {
                    var hallway = layer->Elements.First;
                    while (hallway != null)
                    {
                        var instance = (CLayerInstanceElement*)hallway;
                        var instanceValue = new RValue(instance->Instance);

                        if (instanceValue.Get("currentPos") != null &&
                            (HookUtil.IsEqualToNumeric(instanceValue.Get("currentPos"), 0)))
                        {
                            // Modify the seed
                            var seed = instanceValue.Get("hallseed");
                            if (seed != null && seed->ToString() != "unset")
                            {
                                if (maxCanRun > 3)
                                {
                                    var seedLength = rnsReloaded.ArrayGetLength(seed);
                                    if (seedLength.HasValue && HookUtil.GetNumeric(seedLength.Value) != maxCanRun + 3)
                                    {
                                        var rand = new Random((int)(InventoryUtil.Instance.seed));
                                        //var rand = new Random();
                                        ModifyElementVariable(hallway, "hallseed", ModificationType.InsertToArray, Enumerable.Range(1, maxCanRun - 3).Select(s => new RValue(rand.Next())).ToArray());
                                    }
                                }
                            }

                            // Modify the icons
                            var img = instanceValue.Get("hallsubimg");
                            if (img != null && img->ToString() != "unset")
                            {
                                if (maxCanRun > 3)
                                {
                                    var imgLength = rnsReloaded.ArrayGetLength(img);
                                    if (imgLength.HasValue && HookUtil.GetNumeric(imgLength.Value) != maxCanRun + 3)
                                    {
                                        ModifyElementVariable(hallway, "hallsubimg", ModificationType.InsertToArray, Enumerable.Range(1, maxCanRun - 3).Select(s => new RValue(0)).ToArray());
                                    }
                                    for (var i = 0; i < maxCanRun - 3; i++)
                                    {
                                        ModifyElementVariable(hallway, "hallsubimg", ModificationType.ModifyArray, [new(maxCanRun - 1 + i), new(6)]);
                                    }
                                }
                            }

                        }
                        hallway = hallway->Next;
                    }
                    layer = layer->Next;
                }
            }
        }

        // Toggle the kingdom icons on the route selection screen to only display runnable kingdoms + the pale keep for a random one
        private void ModifyRouteIcons(RValue* buttons)
        {
            // Always allow the random
            *(buttons->Get(0)) = new(1);

            var kingdoms = InventoryUtil.Instance.GetKingdomsAvailableAtNthOrder(CalculateMaxRun());

            if (kingdoms.Contains("hw_nest"))
            {
                *(buttons->Get(1)) = new(1);
            }
            else
            {
                *(buttons->Get(1)) = new(0);
            }

            if (kingdoms.Contains("hw_arsenal"))
            {
                *(buttons->Get(2)) = new(1);
            }
            else
            {
                *(buttons->Get(2)) = new(0);
            }

            if (kingdoms.Contains("hw_lighthouse"))
            {
                *(buttons->Get(3)) = new(1);
            }
            else
            {
                *(buttons->Get(3)) = new(0);
            }

            if (kingdoms.Contains("hw_streets"))
            {
                *(buttons->Get(4)) = new(1);
            }
            else
            {
                *(buttons->Get(4)) = new(0);
            }

            if (kingdoms.Contains("hw_lakeside"))
            {
                *(buttons->Get(5)) = new(1);
            }
            else
            {
                *(buttons->Get(5)) = new(0);
            }

            // Always disallow the extras
            *(buttons->Get(6)) = new(0);
            *(buttons->Get(7)) = new(0);
        }

        // If we are on the route selection screen, update it to match the available kingdoms
        internal RValue* ModifyRouteIcons(CInstance* self,CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Update Route Icons", System.Drawing.Color.DarkOrange);
                    }
                    // Called continously on kingdoms 0-5, so just modify on the last one
                    if (argc == 1 && HookUtil.IsEqualToNumeric(argv[0], 5))
                    {
                        FindLayer("ItemExtra", out var layer);
                        if (layer != null)
                        {
                            var hallway = layer->Elements.First;
                            while (hallway != null)
                            {
                                var instance = (CLayerInstanceElement*)hallway;
                                var instanceValue = new RValue(instance->Instance);

                                var routeIcons = instanceValue.Get("buttonAvailable");
                                if (routeIcons != null && routeIcons->ToString() != "unset")
                                {
                                    ModifyRouteIcons(routeIcons);
                                    returnValue = routeIcons->Get(5);
                                    if (modConfig?.ExtraDebugMessages ?? false)
                                    {
                                        this.logger.PrintMessage("Before Return Update Route Icons", System.Drawing.Color.DarkOrange);
                                    }
                                    return returnValue;
                                }
                                hallway = hallway->Next;
                            }
                        }
                    }
                }
                else
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Before Original Function Route Icons 1", System.Drawing.Color.DarkOrange);
                    }
                    if (this.fixChooseIconsHook != null)
                    {
                        returnValue = this.fixChooseIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                    } else
                    {
                        this.logger.PrintMessage("Unable to call fix choose icons hook", System.Drawing.Color.Red);
                    }
                }
            }
            else
            {
                if (modConfig?.ExtraDebugMessages ?? false)
                {
                    this.logger.PrintMessage("Before Original Function Route Icons 2", System.Drawing.Color.DarkOrange);
                }
                if (this.fixChooseIconsHook != null)
                {
                    returnValue = this.fixChooseIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call fix choose icons hook", System.Drawing.Color.Red);
                }
            }

            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Route Icons", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }

        // Modify the route to take a route that corresponds to the kingdom order
        internal void ModifyRoute(int maxCanRun, InventoryUtil.KingdomFlags visitableKingdoms, bool currentHallwayPosAware)
        {
            if (IsReady(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "stageNameKey", out var element);

                var instance = ((CLayerInstanceElement*)element)->Instance;

                var kingdoms = InventoryUtil.Instance.GetKingdomsAvailableAtNthOrder(maxCanRun);

                var hallkey = rnsReloaded.FindValue(instance, "hallkey");
                var maxKingdoms = InventoryUtil.Instance.maxKingdoms;

                var currentHallwayPos = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "hallwayPos"));

                // Handle the 0th position
                if (!currentHallwayPosAware || currentHallwayPos < 0)
                {
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 0), "hw_outskirts");
                }

                if (maxCanRun == 0)
                {
                    return;
                }

                //var rand = new Random((int)(InventoryUtil.Instance.seed));
                var rand = new Random();

                var unplacedKingdoms = InventoryUtil.Instance.GetKingdomsAvailableAtNthOrder(maxCanRun);

                // Handle the 1st position, trying to encorporate their request
                if (!unplacedKingdoms.Contains(rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, 1))))
                {
                    int randomIndex = rand.Next(unplacedKingdoms.Count());
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 1), unplacedKingdoms[randomIndex]);
                    unplacedKingdoms.Remove(unplacedKingdoms[randomIndex]);
                }
                else
                {
                    unplacedKingdoms.Remove(rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, 1)));
                }

                // Perform initial limiting
                if (currentHallwayPosAware)
                {
                    // Remove kingdoms that are already placed for the list of possible kingdoms
                    for (var i = 2; i <= currentHallwayPos; i++)
                    {
                        unplacedKingdoms.Remove(rnsReloaded.ArrayGetEntry(hallkey, i)->ToString());
                    }

                    // We've already handled pos 0 and 1, so we need to start at least at 2
                    currentHallwayPos = Math.Max(currentHallwayPos + 1, 2);
                }

                for (var i = currentHallwayPosAware ? currentHallwayPos : 2; i <= maxCanRun; i++)
                {
                    var availibleNthKingdoms = InventoryUtil.Instance.GetNthOrderKingdoms(i).Intersect(unplacedKingdoms).ToList();

                    // Prioritize the kingdom of the correct order
                    if (availibleNthKingdoms.Count != 0)
                    {
                        int randomIndex = rand.Next(availibleNthKingdoms.Count());
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, i), availibleNthKingdoms[randomIndex]);
                        unplacedKingdoms.Remove(availibleNthKingdoms[randomIndex]);
                    }
                    else
                    {
                        int randomIndex = rand.Next(unplacedKingdoms.Count());
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, i), unplacedKingdoms[randomIndex]);
                        unplacedKingdoms.Remove(unplacedKingdoms[randomIndex]);
                    }
                }

                // Always set the hallkey length to 9(?) just for easier managing, there are other variables to determine the actual number of runs
                var hallkeyLength = rnsReloaded.ArrayGetLength(hallkey);
                if (hallkeyLength.HasValue && HookUtil.GetNumeric(hallkeyLength.Value) == 6)
                {
                    var endArray = new RValue[3];
                    endArray[0] = *hallkey;
                    rnsReloaded.ExecuteCodeFunction("array_push", null, null, endArray);
                }

                // Place the last 2 where they need to be, if they are visitable 
                var isProgressive = InventoryUtil.Instance.isProgressive;
                if ((visitableKingdoms & InventoryUtil.KingdomFlags.The_Pale_Keep) != 0 && maxCanRun == maxKingdoms && (!isProgressive || InventoryUtil.Instance.ProgressiveRegions >= maxKingdoms + 1))
                {
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "hw_keep");
                }
                else
                {
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "");
                }
                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Moonlit_Pinnacle) != 0 && maxCanRun == maxKingdoms && (!isProgressive || InventoryUtil.Instance.ProgressiveRegions >= maxKingdoms + 2))
                {
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "hw_pinnacle");
                }
                else
                {
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "");
                }
            }

        }

        // Update the route from the start or from the current position + 1
        internal void UpdateRoute(bool currentHallwayPosAware = true)
        {
            this.logger.PrintMessage("updating route", System.Drawing.Color.DarkOrange);
            var visitableKingdoms = InventoryUtil.Instance.AvailableKingdoms;

            var maxCanRun = CalculateMaxRun();
            this.logger.PrintMessage(maxCanRun + "", System.Drawing.Color.DarkOrange);

            ModifyHallSeedAndIcons(maxCanRun);

            ModifyRoute(maxCanRun, visitableKingdoms, currentHallwayPosAware);
        }

        // Create the route such that you only visit kingdoms you are allowed to with your settings and items combo
        internal RValue* CreateRoute(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (modConfig?.ExtraDebugMessages ?? false)
                    {
                        this.logger.PrintMessage("Try Update Route", System.Drawing.Color.DarkOrange);
                    }
                    var isKingdomSanity = InventoryUtil.Instance.isKingdomSanity;
                    var isProgressive = InventoryUtil.Instance.isProgressive;
                    if (isKingdomSanity || isProgressive)
                    {
                        if (modConfig?.ExtraDebugMessages ?? false)
                        {
                            this.logger.PrintMessage("Before Original Function Update Route", System.Drawing.Color.DarkOrange);
                        }
                        if (this.chooseHallsHook != null)
                        {
                            returnValue = this.chooseHallsHook.OriginalFunction(self, other, returnValue, argc, argv);
                        } else
                        {
                            this.logger.PrintMessage("Unable to call choose halls hook", System.Drawing.Color.Red);
                        }
                        this.logger.PrintMessage(HookUtil.PrintHook("create route", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);
                        UpdateRoute(false);

                        if (modConfig?.ExtraDebugMessages ?? false)
                        {
                            this.logger.PrintMessage("Before Return Update Route", System.Drawing.Color.DarkOrange);
                        }
                        return returnValue;
                    }
                }
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Original Function Update Route", System.Drawing.Color.DarkOrange);
            }
            if (this.chooseHallsHook != null)
            {
                returnValue = this.chooseHallsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call choose halls hook", System.Drawing.Color.Red);
            }
            if (modConfig?.ExtraDebugMessages ?? false)
            {
                this.logger.PrintMessage("Before Return Update Route", System.Drawing.Color.DarkOrange);
            }
            return returnValue;
        }
    }
}
