using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using RnSArchipelago.Utils;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class KingdomHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private readonly RouteHandler routeHandler;
        private readonly Config.Config modConfig;
        
        internal IHook<ScriptDelegate>? fixEndIconsHook;
        internal IHook<ScriptDelegate>? changeStartingKingdomBackgroundScriptHook;

        internal KingdomHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, InventoryHandler inventoryHandler, RouteHandler routeHandler, Config.Config modConfig)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.routeHandler = routeHandler;
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