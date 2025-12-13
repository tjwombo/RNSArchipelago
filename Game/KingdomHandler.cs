using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using static RnSArchipelago.Utils.HookUtil;

namespace RnSArchipelago.Game
{
    internal unsafe class KingdomHandler
    {
        private readonly IRNSReloaded rnsReloaded;
        private readonly ILoggerV1 logger;

        internal IHook<ScriptDelegate>? chooseHallsHook;
        internal IHook<ScriptDelegate>? endHallsHook;
        internal IHook<ScriptDelegate>? fixChooseIconsHook;
        internal IHook<ScriptDelegate>? fixEndIconsHook;

        internal KingdomHandler(IRNSReloaded rnsReloaded, ILoggerV1 logger)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;

            InventoryUtil.Instance.UpdateKingdomRoute += UpdateRoute;
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
            for (var i = 1; i <= maxOrder; i++)
            {
                var reachableKingdomsByOrder = InventoryUtil.Instance.GetNthOrderKingdoms(i);
                var runBefore = maxCanRun;
                if (reachableKingdomsByOrder.Count == 0)
                {
                    foreach (var kingdom in unaccountedVisitableKingdoms)
                    {
                        switch (kingdom)
                        {
                            case "hw_nest":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0) {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_arsenal":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                                {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_lakeside":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                                {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_streets":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                                {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_lighthouse":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                                {
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
                        switch (kingdom)
                        {
                            case "hw_nest":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
                                {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_arsenal":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
                                {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_lakeside":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
                                {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_streets":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
                                {
                                    maxCanRun++;
                                }
                                break;
                            case "hw_lighthouse":
                                if ((visitableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
                                {
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
            if (InventoryUtil.Instance.isActive)
            {
                var a = new RValue(self);
                //this.logger.PrintMessage(rnsReloaded.GetString(&a), System.Drawing.Color.Red);

                //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "end", self, returnValue, argc, argv), System.Drawing.Color.Red);
                //this.fixEndIconsHook.Disable();
                HookUtil.FindLayer(rnsReloaded, "RunMenu_Squares", out var layer);
                //this.logger.PrintMessage(layer->Elements.Count + "", System.Drawing.Color.Red);

                CLayerElementBase* hallway = layer->Elements.First;
                if (layer != null)
                {
                    //this.logger.PrintMessage("not null: " + layer->Elements.Count, System.Drawing.Color.Red);
                    //var a = new RValue(self);
                    //this.logger.PrintMessage(rnsReloaded.GetString(&a), System.Drawing.Color.Red);
                    hallway = layer->Elements.First;
                    while (hallway != null)
                    {
                        var instance = (CLayerInstanceElement*)hallway;
                        var instanceValue = new RValue(instance->Instance);


                        var seed = rnsReloaded.FindValue((&instanceValue)->Object, "potY");
                        if (seed != null && seed->ToString() != "unset")
                        {
                            //Console.WriteLine("uhhh");
                            //this.logger.PrintMessage(rnsReloaded.GetString(seed) + "", System.Drawing.Color.RebeccaPurple);
                            //ModifyElementVariable(rnsReloaded, hallway, "potY", ModificationType.ModifyArray, [new(0), new(400)]);
                            //this.logger.PrintMessage(rnsReloaded.GetString(seed) + "", System.Drawing.Color.RebeccaPurple);
                            //var b = new RValue(self);
                            //this.logger.PrintMessage(rnsReloaded.GetString(&b), System.Drawing.Color.Red);
                            returnValue = this.fixEndIconsHook!.OriginalFunction(self, other, returnValue, argc, argv);
                            return returnValue;
                        }
                        //break;
                        //}
                        hallway = hallway->Next;
                    }
                }
            }
            returnValue = this.fixEndIconsHook!.OriginalFunction(self, other, returnValue, argc, argv);

            return returnValue;
        }

        // End the route early if we arent allowed to continue
        private bool EndRouteEarly(CInstance* self)
        {
            var isKingdomSanity = InventoryUtil.Instance.isKingdomSanity;
            var isProgressive = InventoryUtil.Instance.isProgressive;
            var maxKingdoms = InventoryUtil.Instance.maxKingdoms;
            var regionCount = InventoryUtil.Instance.ProgressiveRegions;
            var visitiableKingdomsCount = InventoryUtil.Instance.AvailableKingdomsCount();

            var maxVisitableKingdoms = CalculateMaxRun();

            FindLayer(rnsReloaded, "RunMenu_Blocker", out var layer);
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
                        if (HookUtil.IsEqualToNumeric(currentPos, HookUtil.GetNumeric(notchNumber) - 1))
                        {

                            if (HookUtil.IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("hallwayCurrent"), maxVisitableKingdoms))
                            {
                                var hallkey = rnsReloaded.FindValue(self, "hallkey");
                                return rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 1)) != "hw_keep";
                            } else if (HookUtil.IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("hallwayCurrent"), maxVisitableKingdoms + 1))
                            {
                                var hallkey = rnsReloaded.FindValue(self, "hallkey");
                                return rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, maxVisitableKingdoms + 2)) != "hw_pinnacle";
                            }
                        }
                    }
                    hallway = hallway->Next;
                }
            }
            return false;
        }

        // Increase the route length to the maximum value value
        private void IncreaseRouteLength()
        {
            FindLayer(rnsReloaded, "RunMenu_Blocker", out var layer);
            if (layer != null)
            {
                var hallway = layer->Elements.First;
                while (hallway != null)
                {
                    var instance = (CLayerInstanceElement*)hallway;
                    var instanceValue = new RValue(instance->Instance);

                    
                    if (instanceValue.Get("hallkey") != null && instanceValue.Get("hallkey")->ToString() != "unset")
                    {
                        // Always add 3, so that we dont get the weird Shira visual glitch and account for outskirts
                        HookUtil.ModifyElementVariable(rnsReloaded, hallway, "hallwayNumber", ModificationType.ModifyLiteral, [new(CalculateMaxRun()+3)]);
                        return;
                    }
                    hallway = hallway->Next;
                }
            }
        }

        // Ends the route early if kingdom sanity is enabled, but not enough kingdoms are unlocked, or progressive kingdom count != maxKingdoms
        internal RValue* ManageRouteLength(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (InventoryUtil.Instance.isActive)
            {
                IncreaseRouteLength();

                if (EndRouteEarly(self))
                {
                    rnsReloaded.ExecuteScript("scr_hallwayprogress_make_defeat", self, other, []);
                    return returnValue;
                }
            }
            returnValue = endHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Modify the hallseed and hallway icons for extra visitable kingdoms
        private void ModifyHallSeedAndIcons(int maxCanRun)
        {
            HookUtil.FindLayer(rnsReloaded, "RunMenu_Blocker", out var layer);
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
                                if (rnsReloaded.ArrayGetLength(seed).HasValue && rnsReloaded.ArrayGetLength(seed)!.Value.Real != maxCanRun + 3)
                                {
                                    var rand = new Random((int)(InventoryUtil.Instance.seed));
                                    //var rand = new Random();
                                    ModifyElementVariable(rnsReloaded, hallway, "hallseed", ModificationType.InsertToArray, Enumerable.Range(1, maxCanRun - 3).Select(s => new RValue(rand.Next())).ToArray());
                                }
                            }
                        }

                        // Modify the icons
                        var img = instanceValue.Get("hallsubimg");
                        if (img != null && img->ToString() != "unset")
                        {
                            if (maxCanRun > 3)
                            {
                                if (rnsReloaded.ArrayGetLength(img).HasValue && rnsReloaded.ArrayGetLength(img)!.Value.Real != maxCanRun + 3)
                                {
                                    ModifyElementVariable(rnsReloaded, hallway, "hallsubimg", ModificationType.InsertToArray, Enumerable.Range(1, maxCanRun - 3).Select(s => new RValue(0)).ToArray());
                                }
                                for (var i = 0; i < maxCanRun - 3; i++)
                                {
                                    ModifyElementVariable(rnsReloaded, hallway, "hallsubimg", ModificationType.ModifyArray, [new(maxCanRun - 1 + i), new(6)]);
                                }
                            }
                        }

                    }
                    hallway = hallway->Next;
                }
                layer = layer->Next;
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
            if (InventoryUtil.Instance.isActive)
            {
                // Called continously on kingdoms 0-5, so just modify on the last one
                if (argc == 1 && HookUtil.IsEqualToNumeric(argv[0], 5))
                {
                    FindLayer(rnsReloaded, "ItemExtra", out var layer);
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
                                return returnValue;
                            }
                            hallway = hallway->Next;
                        }
                    }
                }
            }
            else
            {
                returnValue = this.fixChooseIconsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            }


            return returnValue;
        }

        // Modify the route to take a route that corresponds to the kingdom order
        internal void ModifyRoute(int maxCanRun, InventoryUtil.KingdomFlags visitableKingdoms, bool currentHallwayPosAware)
        {
            HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "stageNameKey", out var element);

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
            } else
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
            if (rnsReloaded.ArrayGetLength(hallkey)!.Value.Real == 6)
            {
                var endArray = new RValue[3];
                endArray[0] = *hallkey;
                rnsReloaded.ExecuteCodeFunction("array_push", null, null, endArray);
            }

            // Place the last 2 where they need to be, if they are visitable 
            if ((visitableKingdoms & InventoryUtil.KingdomFlags.The_Pale_Keep) != 0 && maxCanRun >= maxKingdoms)
            {
                rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "hw_keep");
            } else
            {
                rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "");
            }
            if ((visitableKingdoms & InventoryUtil.KingdomFlags.Moonlit_Pinnacle) != 0 && maxCanRun >= maxKingdoms)
            {
                rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "hw_pinnacle");
            } else
            {
                rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "");
            }

        }

        // Update the route from the start or from the current position + 1
        internal void UpdateRoute(bool currentHallwayPosAware = true)
        {
            this.logger.PrintMessage("updating route", System.Drawing.Color.Red);
            var visitableKingdoms = InventoryUtil.Instance.AvailableKingdoms;

            var maxCanRun = CalculateMaxRun();
            this.logger.PrintMessage(maxCanRun + "", System.Drawing.Color.Red);

            ModifyHallSeedAndIcons(maxCanRun);

            ModifyRoute(maxCanRun, visitableKingdoms, currentHallwayPosAware);
        }

        // Create the route such that you only visit kingdoms you are allowed to with your settings and items combo
        internal RValue* CreateRoute(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (InventoryUtil.Instance.isActive)
            {
                var isKingdomSanity = InventoryUtil.Instance.isKingdomSanity;
                var isProgressive = InventoryUtil.Instance.isProgressive;
                if (isKingdomSanity || isProgressive)
                {
                    returnValue = this.chooseHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);
                    this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "create route", self, returnValue, argc, argv), System.Drawing.Color.Red);
                    UpdateRoute(false);

                    return returnValue;
                }
            }
            returnValue = this.chooseHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }
    }
}
