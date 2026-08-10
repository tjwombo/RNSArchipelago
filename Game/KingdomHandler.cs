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
        private readonly InventoryHandler inventoryHandler;
        private readonly Config.Config modConfig;
        
        internal IHook<ScriptDelegate>? fixChooseIconsHook;
        internal IHook<ScriptDelegate>? fixEndIconsHook;
        internal IHook<ScriptDelegate>? changeStartingKingdomBackgroundScriptHook;

        internal string lastVisitedRunType = "";

        internal KingdomHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, InventoryHandler inventoryHandler, Config.Config modConfig)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.modConfig = modConfig;
        }

        // TODO: CANT SEEM TO ACTUALLY MODIFY THE END SCREEN KINGDOM POSITIONS
        internal RValue* ModifyEndScreenIcons(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryHandler.isActive)
                {
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

        // Modify the hallseed and hallway icons for future visitable kingdoms
        internal void ModifyHallSeedAndIconsLength(int maxCanRun)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
               HookUtil.FindElementInLayer("RunMenu_Blocker", "currentPos", out var element);

                if (element == null)
                {
                    return;
                }

                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                if (instanceValue.Get("currentPos") != null && HookUtil.IsEqualToNumeric(instanceValue.Get("currentPos"), 0))
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
                                var rand = new Random(this.inventoryHandler.seed?.GetHashCode() ?? default);
                                HookUtil.ModifyElementVariable(element, "hallseed", ModificationType.InsertToArray, Enumerable.Range(1, maxCanRun - 3).Select(s => new RValue(rand.Next())).ToArray());
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
                            if (imgLength.HasValue && HookUtil.GetNumeric(imgLength.Value) < inventoryHandler.maxKingdoms)
                            {
                                HookUtil.ModifyElementVariable(element, "hallsubimg", ModificationType.InsertToArray, Enumerable.Range(1, (int)(inventoryHandler.maxKingdoms - HookUtil.GetNumeric(imgLength.Value)) + 3).Select(s => new RValue(0)).ToArray());

                                HookUtil.ModifyElementVariable(element, "hallkey", ModificationType.InsertToArray,
                                    Enumerable.Range(1, (int)(inventoryHandler.maxKingdoms - HookUtil.GetNumeric(imgLength.Value)) + 1)
                                                .Select(s => {
                                                    RValue empty = new();
                                                    rnsReloaded.CreateString(&empty, "");
                                                    return empty;
                                                }).ToArray());
                            }

                            for (var i = 0; i < maxCanRun - 3; i++)
                            {
                                HookUtil.ModifyElementVariable(element, "hallsubimg", ModificationType.ModifyArray, [new(maxCanRun - 1 + i), new(6)]);
                            }
                        }
                    }

                }
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
                if (this.inventoryHandler.isActive)
                {
                    HookUtil.FindElementInLayer("ItemExtra", "buttonAvailable", out var element);

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
                        ModifyRouteKingdomIcons(routeIcons, (int)HookUtil.GetNumeric(buttonCount.Value));
                        returnValue = routeIcons->Get((int)HookUtil.GetNumeric(buttonCount.Value) - 1);
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

        // Toggle the kingdom icons on the route selection screen to only display runnable kingdoms + the pale keep for a random one
        internal void ModifyRouteKingdomIcons(RValue* buttons, int buttonCount)
        {
            if (buttonCount >= 6)
            {
                lastVisitedRunType = "kingdom";
                List<string> kingdoms = KingdomUtil.GetRunnableKingdoms(lastVisitedRunType);

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
                List<string> kingdoms = KingdomUtil.GetRunnableKingdoms(lastVisitedRunType);

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

        // Update the background for the starting kingdom when the run starts
        internal RValue* ChangeStartingKingdom(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "stageNameKey", out var element);
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
    }
}