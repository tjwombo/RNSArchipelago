using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using RnSArchipelago.Utils;
using System.Diagnostics.CodeAnalysis;
using Reloaded.Mod.Interfaces;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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

        // Makes it so you can't progress in a class and remove the preview, and restore it in the palette selection
        internal RValue* SetClassAvailability(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.lockVisualClassHook != null)
            {
                returnValue = this.lockVisualClassHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
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
    }
}
