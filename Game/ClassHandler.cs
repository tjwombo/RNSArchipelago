using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using RnSArchipelago.Utils;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class ClassHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly HookUtil hookUtil;
        private readonly InventoryUtil inventoryUtil;

        internal IHook<ScriptDelegate>? lockClassHook;
        internal IHook<ScriptDelegate>? mouseClassHook;
        internal IHook<ScriptDelegate>? stopColorHook;

        // TODO: Use with character ability rando in the future
        internal static long AbilityIdToBaseId(long abilityId)
        {
            return (long)(Math.Floor((abilityId + 1.0) / 7) * 7) - 1;
        }

        internal ClassHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger, HookUtil hookUtil, InventoryUtil inventoryUtil)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.hookUtil = hookUtil;
            this.inventoryUtil = inventoryUtil;
        }

        // Makes it so you can't progress in a class and remove the preview, and restore it in the palette selection
        internal RValue* SetClassAvailability(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.lockClassHook != null)
            {
                returnValue = this.lockClassHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call lock visual class hook", System.Drawing.Color.Red);
            }

            if (!this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return returnValue;

            if (this.inventoryUtil.isActive && this.inventoryUtil.isClassSanity)
            {
                for (var i = 0; i < InventoryUtil.CLASSES.Length; i++)
                {
                    // Character selection
                    if (hookUtil.IsEqualToNumeric(rnsReloaded.FindValue(self, "step"), 1))
                    {
                        if (inventoryUtil.isClassAvailable(i))
                        {
                            *rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "menuAvailable"), i) = new(1);
                            *rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "menuPreview"), i) = new(1);
                        }
                        else
                        {
                            *rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "menuAvailable"), i) = new(0);
                            *rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "menuPreview"), i) = new(0);
                        }
                    }
                    // Palette selection
                    else if (hookUtil.IsEqualToNumeric(rnsReloaded.FindValue(self, "step"), 2))
                    {
                        *rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "menuAvailable"), i) = new(1);
                        *rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "menuPreview"), i) = new(1);
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
            if (this.mouseClassHook != null)
            {
                returnValue = this.mouseClassHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call lock class hook", System.Drawing.Color.Red);
            }

            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return returnValue;

            if (this.inventoryUtil.isActive)
            {
                if (this.inventoryUtil.isClassSanity && !this.inventoryUtil.isClassAvailable((int)this.hookUtil.GetNumeric(rnsReloaded.FindValue(self, "selectedChar"))))
                {
                    var chars = rnsReloaded.FindValue(self, "step");
                    *chars = new(1);
                    var previous = rnsReloaded.FindValue(self, "previousStep");
                    *previous = new(1);

                    //rnsReloaded.ExecuteScript("scr_charselect2_update_chooseclass", self, other, []);
                }
            }
            return returnValue;
        }

        // When using a mouse, make it so that the palettes are not drawn if we are going to loop back to the same state
        internal RValue* StopColorDraw(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryUtil.isActive)
                {
                    if (this.inventoryUtil.isClassSanity && !this.inventoryUtil.isClassAvailable((int)this.hookUtil.GetNumeric(rnsReloaded.FindValue(self, "selectedChar"))))
                    {

                        return returnValue;

                    }
                }
            }
            if (this.stopColorHook != null)
            {
                returnValue = this.stopColorHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call stop color hook", System.Drawing.Color.Red);
            }
            return returnValue;
        }
    }
}