using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using RnSArchipelago.Utils;
using System.Diagnostics.CodeAnalysis;

namespace RnSArchipelago.Game
{
    internal unsafe class ClassHandler
    {
        private readonly WeakReference<IRNSReloaded>? rnsReloadedRef;
        private readonly ILoggerV1 logger;

        internal IHook<ScriptDelegate>? lockClassHook;
        internal IHook<ScriptDelegate>? lockVisualClassHook;
        internal IHook<ScriptDelegate>? stopColorHook;

        internal static long AbilityIdToBaseId(long abilityId)
        {
            return (long)(Math.Floor((abilityId + 1.0) / 7) * 7) - 1;
        }

        private bool IsReady(
            [MaybeNullWhen(false), NotNullWhen(true)] out IRNSReloaded rnsReloaded
        )
        {
            if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out rnsReloaded))
            {
                return rnsReloaded != null;
            }
            this.logger.PrintMessage("Unable to find rnsReloaded in ClassHandler", System.Drawing.Color.Red);
            rnsReloaded = null;
            return false;
        }

        internal ClassHandler(WeakReference<IRNSReloaded>? rnsReloadedRef, ILoggerV1 logger)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
        }

        // Visually lock the classes that are not available
        internal RValue* LockVisualClass(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.lockVisualClassHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (InventoryUtil.Instance.isClassSanity && HookUtil.IsEqualToNumeric(rnsReloaded.FindValue(self, "step"), 1))
                    {
                        for (var i = 0; i < InventoryUtil.Instance.AvailableClassesCount; i++)
                        {
                            if (!InventoryUtil.Instance.isClassAvailable(i))
                            {
                                var chars = rnsReloaded.FindValue(self, "menuAvailable");
                                *chars->Get(i) = new(0);
                                var preview = rnsReloaded.FindValue(self, "menuPreview");
                                *preview->Get(i) = new(0);
                            }
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
            returnValue = this.lockClassHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (InventoryUtil.Instance.isClassSanity && !InventoryUtil.Instance.isClassAvailable((int)HookUtil.GetNumeric(rnsReloaded.FindValue(self, "selectedChar"))))
                    {
                        var chars = rnsReloaded.FindValue(self, "step");
                        *chars = new(1);
                        var previous = rnsReloaded.FindValue(self, "previousStep");
                        *previous = new(1);

                        //rnsReloaded.ExecuteScript("scr_charselect2_update_chooseclass", self, other, []);
                    }
                }
            }
            return returnValue;
        }

        // Make it so that the palettes are not drawn if we are going to loop back to the same state
        internal RValue* StopColorDraw(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (IsReady(out var rnsReloaded))
            {
                if (InventoryUtil.Instance.isActive)
                {
                    if (InventoryUtil.Instance.isClassSanity && !InventoryUtil.Instance.isClassAvailable((int)HookUtil.GetNumeric(rnsReloaded.FindValue(self, "selectedChar"))))
                    {
                        return returnValue;
                    }
                }
            }
            returnValue = this.stopColorHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }
    }
}
