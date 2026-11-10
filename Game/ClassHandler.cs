using Reloaded.Mod.Interfaces;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class ClassHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly HookHandler hookHandler;
        private readonly InventoryHandler inventoryHandler;

        public ClassHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, HookHandler hookHandler, InventoryHandler inventoryHandler)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.hookHandler = hookHandler;
            this.inventoryHandler = inventoryHandler;
        }

        // TODO: Use with character ability rando in the future
        internal static long AbilityIdToBaseId(long abilityId)
        {
            return (long)(Math.Floor((abilityId + 1.0) / 7) * 7) - 1;
        }

        // Makes it so you can't progress in a class and remove the preview, and restore it in the palette selection
        internal RValue* SetClassAvailability(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (hookHandler.lockClassHook != null)
            {
                returnValue = hookHandler.lockClassHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call lock visual class hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (inventoryHandler.isActive && inventoryHandler.isClassSanity)
                {
                    for (var i = 0; i < InventoryHandler.CLASSES.Length; i++)
                    {
                        // Character selection
                        if (rnsReloaded.FindValue(self, "step")->ToInt() == 1)
                        {
                            if (inventoryHandler.IsClassAvailable(i))
                            {
                                *rnsReloaded.FindValue(self, "menuAvailable")->Get(i) = 1;
                                *rnsReloaded.FindValue(self, "menuPreview")->Get(i) = 1;
                            }
                            else
                            {
                                *rnsReloaded.FindValue(self, "menuAvailable")->Get(i) = 0;
                                *rnsReloaded.FindValue(self, "menuPreview")->Get(i) = 0;
                            }
                        }
                        // Palette selection
                        else if (rnsReloaded.FindValue(self, "step")->ToInt() == 2)
                        {
                            *rnsReloaded.FindValue(self, "menuAvailable")->Get(i) = 1;
                            *rnsReloaded.FindValue(self, "menuPreview")->Get(i) = 1;
                        }
                    }
                }
            }

            return returnValue;
        }

        // Below are mouse restrictions as they are operating at a different time than keyboard/controller, but the other controls should also be affected by below (they just get stopped earlier normally)

        // TODO: Find a better function to hook that actually changes the step, which will hopefully stop the flash unlock
        // When using a mouse, make it so that in the state machine if we select a locked class, instead of going to the next state, we loop back
        internal RValue* LockClass(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (hookHandler.mouseClassHook != null)
            {
                returnValue = hookHandler.mouseClassHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call lock class hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (inventoryHandler.isActive)
                {
                    if (inventoryHandler.isClassSanity && !inventoryHandler.IsClassAvailable(rnsReloaded.FindValue(self, "selectedChar")->ToInt()))
                    {
                        var chars = rnsReloaded.FindValue(self, "step");
                        *chars = 1;
                        var previous = rnsReloaded.FindValue(self, "previousStep");
                        *previous = 1;
                    }
                }
            }

            return returnValue;
        }

        // When using a mouse, make it so that the palettes are not drawn if we are going to loop back to the same state
        internal RValue* StopColorDraw(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (inventoryHandler.isActive)
                {
                    if (inventoryHandler.isClassSanity && !inventoryHandler.IsClassAvailable(rnsReloaded.FindValue(self, "selectedChar")->ToInt()))
                    {
                        return returnValue;
                    }
                }
            }

            if (hookHandler.stopColorHook != null)
            {
                returnValue = hookHandler.stopColorHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call stop color hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }
    }
}