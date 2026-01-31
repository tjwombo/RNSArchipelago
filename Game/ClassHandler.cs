using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using RnSArchipelago.Utils;
using System.Diagnostics.CodeAnalysis;
using Reloaded.Mod.Interfaces;

namespace RnSArchipelago.Game
{
    internal unsafe class ClassHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly HookUtil hookUtil;
        private readonly InventoryUtil inventoryUtil;

        internal IHook<ScriptDelegate>? lockClassHook;
        internal IHook<ScriptDelegate>? lockVisualClassHook;
        internal IHook<ScriptDelegate>? stopColorHook;

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

        // Visually lock the classes that are not available
        internal RValue* LockVisualClass(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.lockVisualClassHook != null)
            {
                returnValue = this.lockVisualClassHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call lock visual class hook", System.Drawing.Color.Red);
            }

            if (!this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return returnValue;
            
            if (this.inventoryUtil.isActive)
            {
                if (this.inventoryUtil.isClassSanity && this.hookUtil.IsEqualToNumeric(rnsReloaded.FindValue(self, "step"), 1))
                {
                    for (var i = 0; i < this.inventoryUtil.AvailableClassesCount; i++)
                    {
                        if (!this.inventoryUtil.isClassAvailable(i))
                        {
                            var chars = rnsReloaded.FindValue(self, "menuAvailable");
                            *chars->Get(i) = new(0);
                            var preview = rnsReloaded.FindValue(self, "menuPreview");
                            *preview->Get(i) = new(0);
                        }
                    }
                }
            }
            return returnValue;
        }

        // TODO: Find a better function to hook that actually changes the step, which will hopefully stop the flash unlock
        // Make it so that in the state machine if we select a locked class, instead of going to the next state, we loop back
        internal RValue* LockClass(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.lockClassHook != null)
            {
                returnValue = this.lockClassHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
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

        // Make it so that the palettes are not drawn if we are going to loop back to the same state
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
            } else
            {
                this.logger.PrintMessage("Unable to call stop color hook", System.Drawing.Color.Red);
            }
            return returnValue;
        }
    }
}
