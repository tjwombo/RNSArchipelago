using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using RnSArchipelago.Connection;
using RnSArchipelago.Utils;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

using static RnSArchipelago.Utils.HookUtil;

namespace RnSArchipelago.Game
{
    internal unsafe class KingdomHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly HookUtil hookUtil;
        private readonly InventoryUtil inventoryUtil;
        private readonly Config.Config modConfig;
        private readonly LocationHandler locationHandler;
        private readonly ArchipelagoConnection conn;

        internal IHook<ScriptDelegate>? chooseHallsHook;
        internal IHook<ScriptDelegate>? endHallsHook;
        internal IHook<ScriptDelegate>? fixChooseIconsHook;
        internal IHook<ScriptDelegate>? fixEndIconsHook;
        internal IHook<ScriptDelegate>? changeStartingKingdomBackgroundScriptHook;

        private string lastVisitedRunType = "";

        internal KingdomHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, HookUtil hookUtil, InventoryUtil inventoryUtil, Config.Config modConfig, LocationHandler locationHandler, ArchipelagoConnection conn)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.hookUtil = hookUtil;
            this.inventoryUtil = inventoryUtil;
            this.modConfig = modConfig;
            this.locationHandler = locationHandler;
            this.conn = conn;

            this.inventoryUtil.UpdateHallwayOnItemRecieve += OnKingdomUpdate;
        }

        // Gets the kingdoms you can visit for your run, excluding the ending hallways
        internal List<string> GetRunnableKingdoms()
        {
            if (this.inventoryUtil.RunType == InventoryUtil.RunTypeSetting.Combined)
            {
                return this.inventoryUtil.GetChaosKingdomsAvailable();
            }
            else if (this.inventoryUtil.RunType == InventoryUtil.RunTypeSetting.Kingdom)
            {
                return this.inventoryUtil.GetKingdomKingdomsAvailable();
            }
            else if (this.inventoryUtil.RunType == InventoryUtil.RunTypeSetting.Extra)
            {
                return this.inventoryUtil.GetExtraKingdomsAvailable();
            }
            else if (this.inventoryUtil.RunType == InventoryUtil.RunTypeSetting.Either)
            {
                List<string> kingdoms;

                // Try our tab and if nothing is found go to the other tab
                if (lastVisitedRunType == "kingdom")
                {
                    kingdoms = this.inventoryUtil.GetKingdomKingdomsAvailable();
                    if (kingdoms.Count > 0)
                    {
                        lastVisitedRunType = "kingdom";
                        return kingdoms;
                    }
                    kingdoms = this.inventoryUtil.GetExtraKingdomsAvailable();
                    lastVisitedRunType = "extra";
                    return kingdoms;
                }
                else if (lastVisitedRunType == "extra")
                {
                    kingdoms = this.inventoryUtil.GetExtraKingdomsAvailable();
                    if (kingdoms.Count > 0)
                    {
                        lastVisitedRunType = "extra";
                        return kingdoms;
                    }

                    kingdoms = this.inventoryUtil.GetKingdomKingdomsAvailable();
                    lastVisitedRunType = "kingdom";
                    return kingdoms;
                }
                // Otherwise default to assuming it was a kingdom tab
                else
                {
                    kingdoms = this.inventoryUtil.GetKingdomKingdomsAvailable();
                    if (kingdoms.Count > 0)
                    {
                        lastVisitedRunType = "kingdom";
                        return kingdoms;
                    }

                    kingdoms = this.inventoryUtil.GetExtraKingdomsAvailable();
                    lastVisitedRunType = "extra";
                    return kingdoms;
                }
            }

            return [];
        }

        // Gets the kingdoms you can visit for your run at a given kingdom order, excluding the ending hallways
        internal List<string> GetOrderedRunnableKingdoms(int n)
        {
            if (this.inventoryUtil.RunType == InventoryUtil.RunTypeSetting.Combined)
            {
                return this.inventoryUtil.GetChaosKingdomsAvailable(n);
            }
            else if (this.inventoryUtil.RunType == InventoryUtil.RunTypeSetting.Kingdom || (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Extra && lastVisitedRunType == "kingdom"))
            {
                return this.inventoryUtil.GetKingdomKingdomsAvailable(n);
            }
            else if (this.inventoryUtil.RunType == InventoryUtil.RunTypeSetting.Extra || (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Extra && lastVisitedRunType == "extra"))
            {
                return this.inventoryUtil.GetExtraKingdomsAvailable(n);
            }

            return [];
        }

        // TODO: CANT SEEM TO ACTUALLY MODIFY THE END SCREEN KINGDOM POSITIONS
        internal RValue* ModifyEndScreenIcons(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryUtil.isActive)
                {
                    var a = new RValue(self);
                    //this.logger.PrintMessage(rnsReloaded.GetString(&a), System.Drawing.Color.DarkOrange);

                    //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "end", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);
                    //this.fixEndIconsHook.Disable();
                    this.hookUtil.FindLayer("RunMenu_Squares", out var layer);
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
                                if (this.fixEndIconsHook != null)
                                {
                                    returnValue = this.fixEndIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                                }
                                else
                                {
                                    this.logger.PrintMessage("Unable to call fix end icons hook", System.Drawing.Color.Red);
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

            if (this.fixEndIconsHook != null)
            {
                returnValue = this.fixEndIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call fix end icons hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // End the route early if we arent allowed to continue
        private bool EndRouteEarly()
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {

                hookUtil.FindElementInLayer("RunMenu_Blocker", "hallkey", out var element);

                if (element == null)
                {
                    return false;
                }

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                var hallkey = instanceValue.Get("hallkey");

                var kingdoms = GetRunnableKingdoms();
                var maxVisitableKingdoms = kingdoms.Count;
                if (kingdoms.Contains("hw_outskirts"))
                {
                    maxVisitableKingdoms--;
                }
                if (kingdoms.Contains("hw_geode"))
                {
                    maxVisitableKingdoms--;
                }

                if (this.inventoryUtil.isProgressive)
                {
                    maxVisitableKingdoms = (int)Math.Min(maxVisitableKingdoms, this.inventoryUtil.ProgressiveRegions);
                }

                maxVisitableKingdoms = (int)Math.Min(maxVisitableKingdoms, this.inventoryUtil.maxKingdoms);

                var hallwayNumber = this.hookUtil.GetNumeric(rnsReloaded.utils.GetGlobalVar("hallwayCurrent")); // 0 is Kingdom Outskirts / Crack in the Geode

                if (hallwayNumber < maxVisitableKingdoms)
                {
                    return false;
                }

                var currentPos = instanceValue.Get("currentPos");
                var notchNumber = instanceValue.Get("notchNumber");

                if (currentPos != null && notchNumber != null)
                {
                    // Check to see if we are at the last notch in the hallway
                    if (this.hookUtil.IsEqualToNumeric(currentPos, this.hookUtil.GetNumeric(notchNumber) - 1))
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
                hookUtil.FindElementInLayer("RunMenu_Blocker", "hallkey", out var element);

                if (element == null)
                {
                   return;
                }

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                var hallkey = instanceValue.Get("hallkey");

                var kingdoms = GetRunnableKingdoms();
                var maxCanRun = kingdoms.Count;
                if (kingdoms.Contains("hw_outskirts"))
                {
                    maxCanRun--;
                }
                if (kingdoms.Contains("hw_geode"))
                {
                    maxCanRun--;
                }
                maxCanRun = (int)Math.Min(maxCanRun, this.inventoryUtil.maxKingdoms);

                if (this.inventoryUtil.isProgressive)
                {
                    maxCanRun = (int)Math.Min(maxCanRun, this.inventoryUtil.ProgressiveRegions);
                }

                if (hallkey != null && hallkey->ToString() != "unset" && this.hookUtil.GetNumeric(instanceValue.Get("hallwayNumber")) != maxCanRun + 3)
                {
                    // Always add 3, so that we dont get the weird Shira visual glitch and account for outskirts
                    this.hookUtil.ModifyElementVariable(element, "hallwayNumber", ModificationType.ModifyLiteral, [new(maxCanRun + 3)]);
                }
            }
        }

        // Update the kingdoms we visit and the number of kingdoms we should be visiting
        internal void OnKingdomUpdate(bool currentHallwayPosAware = true)
        {
            UpdateRoute(currentHallwayPosAware);

            UpdateRouteLength();
        }

        // Ends the route early if kingdom sanity is enabled, but not enough kingdoms are unlocked, or progressive kingdom count != maxKingdoms
        internal RValue* ManageRouteLength(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryUtil.isActive)
                {
                    // Backup send location in case they disconnected during the fight
                    this.hookUtil.FindElementInLayer("RunMenu_Blocker", "currentPos", out var element);
                    if (element != null)
                    {
                        var instance = new RValue(((CLayerInstanceElement*)element)->Instance);
                        var currentPos = this.hookUtil.GetNumeric(instance.Get("currentPos")); // Wasn't playing well with IsEqualToNumeric
                        var hallwayPos = this.hookUtil.GetNumeric(instance.Get("hallwayPos"));
                        var index = this.hookUtil.GetNumeric(instance.Get("currentPos"));

                        // First check is to prevent shop, second is general transitions, and last is final boss
                        if ((currentPos != 0 || hallwayPos != 0) && index != -1 && (hallwayPos != this.inventoryUtil.maxKingdoms + 2 && currentPos == 0))
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

        // Modify the hallseed and hallway icons for extra visitable kingdoms
        private void ModifyHallSeedAndIconsLength(int maxCanRun)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
               hookUtil.FindElementInLayer("RunMenu_Blocker", "currentPos", out var element);

                if (element == null)
                {
                    return;
                }

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                if (instanceValue.Get("currentPos") != null && this.hookUtil.IsEqualToNumeric(instanceValue.Get("currentPos"), 0))
                {
                    // Modify the seed
                    var seed = instanceValue.Get("hallseed");
                    if (seed != null && seed->ToString() != "unset")
                    {
                        if (maxCanRun > 3)
                        {
                            var seedLength = rnsReloaded.ArrayGetLength(seed);
                            if (seedLength.HasValue && this.hookUtil.GetNumeric(seedLength.Value) != maxCanRun + 3)
                            {
                                var rand = new Random(this.inventoryUtil.seed?.GetHashCode() ?? default);
                                this.hookUtil.ModifyElementVariable(element, "hallseed", ModificationType.InsertToArray, Enumerable.Range(1, maxCanRun - 3).Select(s => new RValue(rand.Next())).ToArray());
                            }
                        }
                    }

                    // Modify the icons length
                    var img = instanceValue.Get("hallsubimg");
                    if (img != null && img->ToString() != "unset")
                    {
                        if (maxCanRun > 3)
                        {
                            var imgLength = rnsReloaded.ArrayGetLength(img);
                            if (imgLength.HasValue && this.hookUtil.GetNumeric(imgLength.Value) < inventoryUtil.maxKingdoms)
                            {
                                this.hookUtil.ModifyElementVariable(element, "hallsubimg", ModificationType.InsertToArray, Enumerable.Range(1, (int)(inventoryUtil.maxKingdoms - this.hookUtil.GetNumeric(imgLength.Value)) + 3).Select(s => new RValue(0)).ToArray());

                                this.hookUtil.ModifyElementVariable(element, "hallkey", ModificationType.InsertToArray,
                                    Enumerable.Range(1, (int)(inventoryUtil.maxKingdoms - this.hookUtil.GetNumeric(imgLength.Value)) + 1)
                                                .Select(s => {
                                                    RValue empty = new();
                                                    rnsReloaded.CreateString(&empty, "");
                                                    return empty;
                                                }).ToArray());
                            }

                            for (var i = 0; i < maxCanRun - 3; i++)
                            {
                                this.hookUtil.ModifyElementVariable(element, "hallsubimg", ModificationType.ModifyArray, [new(maxCanRun - 1 + i), new(6)]);
                            }
                        }
                    }

                }
            }
        }

        // Toggle the kingdom icons on the route selection screen to only display runnable kingdoms + the pale keep for a random one
        private void ModifyRouteIcons(RValue* buttons, int buttonCount)
        {

            if (buttonCount >= 6)
            {
                lastVisitedRunType = "kingdom";
                List<string> kingdoms = GetRunnableKingdoms();

                if (kingdoms.Contains("hw_outskirts"))
                {
                    *(buttons->Get(0)) = new(1);
                }
                else
                {
                    *(buttons->Get(0)) = new(0);
                }

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
            else if (buttonCount == 4)
            {
                lastVisitedRunType = "extra";
                List<string> kingdoms = GetRunnableKingdoms();

                if (kingdoms.Contains("hw_geode"))
                {
                    *(buttons->Get(0)) = new(1);
                }
                else
                {
                    *(buttons->Get(0)) = new(0);
                }

                if (kingdoms.Contains("hw_sanct"))
                {
                    *(buttons->Get(1)) = new(1);
                }
                else
                {
                    *(buttons->Get(1)) = new(0);
                }

                if (kingdoms.Contains("hw_depths"))
                {
                    *(buttons->Get(2)) = new(1);
                }
                else
                {
                    *(buttons->Get(2)) = new(0);
                }

                if (kingdoms.Contains("hw_aurum"))
                {
                    *(buttons->Get(3)) = new(1);
                }
                else
                {
                    *(buttons->Get(3)) = new(0);
                }
            }
            else if (buttonCount == 2)
            {
                // true random
                *(buttons->Get(0)) = new(0);
                // chaotic random
                *(buttons->Get(1)) = new(0);
            }
        }

        // If we are on the route selection screen, update it to match the available kingdoms
        internal RValue* ModifyRouteIcons(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.fixChooseIconsHook != null)
                {
                    returnValue = this.fixChooseIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call fix choose icons hook", System.Drawing.Color.Red);
                }
                if (this.inventoryUtil.isActive)
                {
                    hookUtil.FindElementInLayer("ItemExtra", "buttonAvailable", out var element);

                    if (element == null)
                    {
                        return returnValue;
                    }

                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    var routeIcons = instanceValue.Get("buttonAvailable");
                    var buttonCount = rnsReloaded.ArrayGetLength(routeIcons);

                    if (routeIcons != null && routeIcons->ToString() != "unset" && buttonCount.HasValue)
                    {
                        ModifyRouteIcons(routeIcons, (int)hookUtil.GetNumeric(buttonCount.Value));
                        returnValue = routeIcons->Get((int)hookUtil.GetNumeric(buttonCount.Value) - 1);
                    }
                }
                return returnValue;
            }
            else
            {
                if (this.fixChooseIconsHook != null)
                {
                    returnValue = this.fixChooseIconsHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call fix choose icons hook", System.Drawing.Color.Red);
                }
            }

            return returnValue;
        }

        private readonly string[] locationSuffix = [" Battle 1", " Battle 2", " Battle 3", " Chest", " Boss"];

        // Return the index of the kingdom that is chosen weighted randomly prioritizing kingdoms with more checks remaining
        private int GetWeightedKingdom(List<string> kingdoms)
        {
            if (conn.session != null) {
                var locations = conn.session.Locations.AllMissingLocations;
                var character = this.hookUtil.GetClass();

                var weights = new int[kingdoms.Count];
                double sum = 0;

                // Assign weights for each kingdom
                for (var i = 0; i < kingdoms.Count; i++)
                {
                    weights[i] = 1;
                    sum += 1;

                    var kingdom = InventoryUtil.KingdomNotchToLocationName(kingdoms[i]);

                    // Add weights for each of the standard locations
                    for (var j = 0; j < locationSuffix.Length; j++)
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + locationSuffix[j])))
                        {
                            weights[i] += 1;
                            sum += 1;
                        }

                        if (character != "")
                        {
                            if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + locationSuffix[j] + " - " + character)))
                            {
                                weights[i] += 2;
                                sum += 2;
                            }
                        }
                    }

                    // Chest for chest item positions
                    for (var j = 0; j < LocationHandler.CHEST_POSITIONS.Length; j++)
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + " Chest " + LocationHandler.CHEST_POSITIONS[j])))
                        {
                            weights[i] += 1;
                            sum += 1;
                        }
                    }

                    // Check for regional shop item positions
                    for (var j = 0; j < LocationHandler.SHOP_POSITIONS.Length; j++)
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + " Chest " + LocationHandler.SHOP_POSITIONS[j])))
                        {
                            weights[i] += 1;
                            sum += 1;
                        }
                    }

                    // Check for Shira/Witch
                    if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom)))
                    {
                        weights[i] += 5;
                        sum += 5;
                    }

                    if (character != "")
                    {
                        if (locations.Contains(conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, kingdom + " - " + character)))
                        {
                            weights[i] += 10;
                            sum += 10;
                        }
                    }
                }

                var rand = new Random();
                double value = rand.NextDouble();

                for (var i = 0; i < kingdoms.Count; i++)
                {
                    if (weights.Take(i+1).Sum() / sum >= value)
                    {
                        return i;
                    } 
                }
            }
            return 0;
        }

        // Modify the route to take a route that corresponds to the kingdom order
        internal void ModifyRoute(int maxCanRun, InventoryUtil.KingdomFlags visitableKingdoms, bool currentHallwayPosAware)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                this.hookUtil.FindElementInLayer("RunMenu_Blocker", "stageNameKey", out var element);

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                var unplacedKingdoms = GetRunnableKingdoms();

                var hallkey = instanceValue.Get("hallkey");
                var hallsubimg = instanceValue.Get("hallsubimg");
                var maxKingdoms = this.inventoryUtil.maxKingdoms;

                var currentHallwayPos = (int)this.hookUtil.GetNumeric(instanceValue.Get("hallwayPos"));
                var currentPos = (int)this.hookUtil.GetNumeric(instanceValue.Get("currentPos"));

                // Handle the 0th position
                if (!currentHallwayPosAware || currentHallwayPos < 0 || (currentPos <= 0 && currentHallwayPos == 0))
                {
                    var hallsubimgValue = (int)hookUtil.GetNumeric(rnsReloaded.ArrayGetLength(hallsubimg)!.Value);

                    if (unplacedKingdoms.Contains("hw_outskirts") && unplacedKingdoms.Contains("hw_geode"))
                    {
                        if (lastVisitedRunType == "kingdom")
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 0), "hw_outskirts");

                            *rnsReloaded.ArrayGetEntry(hallsubimg, 0) = new(7);
                            *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 2) = new(0);
                            *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 1) = new(0);
                        }
                        else
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 0), "hw_geode");

                            *rnsReloaded.ArrayGetEntry(hallsubimg, 0) = new(9);
                            *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 2) = new(13);
                            *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 1) = new(13);
                        }
                    }
                    else if (unplacedKingdoms.Contains("hw_outskirts"))
                    {
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 0), "hw_outskirts");

                        *rnsReloaded.ArrayGetEntry(hallsubimg, 0) = new(7);
                        *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 2) = new(0);
                        *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 1) = new(0);
                    }
                    else if (unplacedKingdoms.Contains("hw_geode"))
                    {
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 0), "hw_geode");

                        *rnsReloaded.ArrayGetEntry(hallsubimg, 0) = new(9);
                        *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 2) = new(13);
                        *rnsReloaded.ArrayGetEntry(hallsubimg, hallsubimgValue - 1) = new(13);
                    }

                    // Generate the hallway data
                    if (rnsReloaded.ArrayGetEntry(hallkey, 0)->ToString().Equals("hw_outskirts"))
                    {
                        rnsReloaded.ExecuteScript("scr_hallwaygen_outskirts", instance->Instance, null, []);
                    } else if (rnsReloaded.ArrayGetEntry(hallkey, 0)->ToString().Equals("hw_geode"))
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
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 1), "");
                    return;
                }

                // Handle the 1st position, trying to encorporate their request
                if (!unplacedKingdoms.Contains(rnsReloaded.GetString(rnsReloaded.ArrayGetEntry(hallkey, 1))))
                {
                    int selectedIndex = GetWeightedKingdom(unplacedKingdoms);
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, 1), unplacedKingdoms[selectedIndex]);
                    unplacedKingdoms.Remove(unplacedKingdoms[selectedIndex]);
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
                    var availibleNthKingdoms = GetOrderedRunnableKingdoms(i).Intersect(unplacedKingdoms).ToList();

                    // Prioritize the kingdom of the correct order
                    if (availibleNthKingdoms.Count != 0)
                    {
                        int selectedIndex = GetWeightedKingdom(availibleNthKingdoms);
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, i), availibleNthKingdoms[selectedIndex]);
                        unplacedKingdoms.Remove(availibleNthKingdoms[selectedIndex]);
                    }
                    else
                    {
                        int selectedIndex = GetWeightedKingdom(unplacedKingdoms);
                        rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, i), unplacedKingdoms[selectedIndex]);
                        unplacedKingdoms.Remove(unplacedKingdoms[selectedIndex]);
                    }
                }

                // Always set the hallkey length to 9 just for easier managing, there are other variables to determine the actual number of runs
                var hallkeyLength = rnsReloaded.ArrayGetLength(hallkey);
                if (hallkeyLength.HasValue && this.hookUtil.GetNumeric(hallkeyLength.Value) == 6)
                {
                    var endArray = new RValue[3];
                    endArray[0] = *hallkey;
                    rnsReloaded.ExecuteCodeFunction("array_push", null, null, endArray);
                }

                // TODO: MAKE BETTER
                // Place the last 2 where they need to be, if they are visitable 
                var isProgressive = this.inventoryUtil.isProgressive;
                if (maxCanRun == maxKingdoms && (!isProgressive || this.inventoryUtil.ProgressiveRegions >= maxKingdoms + 1))
                {
                    if (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Kingdom || (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Either && lastVisitedRunType == "kingdom"))
                    {
                        if ((visitableKingdoms & InventoryUtil.KingdomFlags.The_Pale_Keep) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "hw_keep");
                        }
                        else
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "");
                        }
                    }
                    else if (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Extra || (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Either && lastVisitedRunType == "extra"))
                    {
                        if ((visitableKingdoms & InventoryUtil.KingdomFlags.Looping_Hallway) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "hw_darkhall");
                        }
                        else
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "");
                        }
                    }
                    else if (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Combined)
                    {
                        if ((visitableKingdoms & InventoryUtil.KingdomFlags.The_Pale_Keep) != 0 && (visitableKingdoms & InventoryUtil.KingdomFlags.Looping_Hallway) != 0)
                        {
                            List<string> kingdoms = ["hw_keep", "hw_darkhall"];
                            int selectedIndex = GetWeightedKingdom(kingdoms);
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), kingdoms[selectedIndex]);
                        }
                        else if ((visitableKingdoms & InventoryUtil.KingdomFlags.The_Pale_Keep) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "hw_keep");
                        }
                        else if ((visitableKingdoms & InventoryUtil.KingdomFlags.Looping_Hallway) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "hw_darkhall");
                        }
                        else
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "");
                        }
                    }
                }
                else
                {
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), "");
                }

                if (maxCanRun == maxKingdoms && (!isProgressive || this.inventoryUtil.ProgressiveRegions >= maxKingdoms + 2))
                {
                    if (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Kingdom || (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Either && lastVisitedRunType == "kingdom"))
                    {
                        if ((visitableKingdoms & InventoryUtil.KingdomFlags.Moonlit_Pinnacle) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "hw_pinnacle");
                        }
                        else
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "");
                        }
                    }
                    else if (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Extra || (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Either && lastVisitedRunType == "extra"))
                    {
                        if ((visitableKingdoms & InventoryUtil.KingdomFlags.Reflecting_Pool) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "hw_reflection");
                        }
                        else
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "");
                        }
                    }
                    else if (this.inventoryUtil.run_type == InventoryUtil.RunTypeSetting.Combined)
                    {
                        if ((visitableKingdoms & InventoryUtil.KingdomFlags.Moonlit_Pinnacle) != 0 && (visitableKingdoms & InventoryUtil.KingdomFlags.Reflecting_Pool) != 0)
                        {
                            List<string> kingdoms = ["hw_pinnacle", "hw_reflection"];
                            int selectedIndex = GetWeightedKingdom(kingdoms);
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 1), kingdoms[selectedIndex]);
                        }
                        else if ((visitableKingdoms & InventoryUtil.KingdomFlags.Moonlit_Pinnacle) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "hw_pinnacle");
                        }
                        else if ((visitableKingdoms & InventoryUtil.KingdomFlags.Reflecting_Pool) != 0)
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "hw_reflection");
                        }
                        else
                        {
                            rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "");
                        }
                    }
                }
                else
                {
                    rnsReloaded.CreateString(rnsReloaded.ArrayGetEntry(hallkey, maxCanRun + 2), "");
                }
            }

        }

        // Update the route from the start or from the current position + 1
        internal void UpdateRoute(bool currentHallwayPosAware)
        {
            this.logger.PrintMessage("Updating route", System.Drawing.Color.DarkOrange);

            var visitableKingdoms = this.inventoryUtil.AvailableKingdoms;

            var kingdoms = GetRunnableKingdoms();
            var maxCanRun = kingdoms.Count;
            if (kingdoms.Contains("hw_outskirts"))
            {
                maxCanRun--;
            }
            if (kingdoms.Contains("hw_geode"))
            {
                maxCanRun--;
            }
            maxCanRun = (int)Math.Min(maxCanRun, this.inventoryUtil.maxKingdoms);

            if (this.inventoryUtil.isProgressive)
            {
                maxCanRun = (int)Math.Min(maxCanRun, this.inventoryUtil.ProgressiveRegions);
            }

            this.logger.PrintMessage("Route length: " + maxCanRun, System.Drawing.Color.DarkOrange);

            ModifyHallSeedAndIconsLength(maxCanRun);

            ModifyRoute(maxCanRun, visitableKingdoms, currentHallwayPosAware);
        }

        // Update the background for the starting kingdom when the run starts
        internal RValue* ChangeStartingKingdom(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                this.hookUtil.FindElementInLayer("RunMenu_Blocker", "stageNameKey", out var element);
                var instance = ((CLayerInstanceElement*)element)->Instance;

                rnsReloaded.ExecuteScript("scr_hallwayprogress_change_stage", instance, null, []);
            }

            if (this.changeStartingKingdomBackgroundScriptHook != null)
            {
                returnValue = this.changeStartingKingdomBackgroundScriptHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call run start hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Create the route such that you only visit kingdoms you are allowed to with your settings and items combo
        internal RValue* CreateRoute(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryUtil.isActive)
                {
                    var isKingdomSanity = this.inventoryUtil.isKingdomSanity;
                    var isProgressive = this.inventoryUtil.isProgressive;
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

                        OnKingdomUpdate(false);

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
    }
}