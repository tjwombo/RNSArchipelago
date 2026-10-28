using RnSArchipelago.Utils;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;

namespace RnSArchipelago.Game
{
    internal unsafe class SaveHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;

        internal IHook<ScriptDelegate>? originalUnlockHook;

        private static readonly string[] keys = { "hardDiff", "lunarDiff", "extraMode", "randomMode", "charSpBlade", "charSniper", "charBruiser", "charDefender", "charAncient", "charHammer", "charPyro", "charGunner", "charShadow" };

        private readonly long[] originalUnlocks = new long[keys.Length];
        private RValue* unlockKeys;

        internal SaveHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
        }

        internal RValue* RecordSaveInfo(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.originalUnlockHook != null)
            {
                returnValue = this.originalUnlockHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call original unlock hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {

                unlockKeys = rnsReloaded.utils.GetGlobalVar("unlOtherKey");
                var keysLength = unlockKeys->ArrayLength();
                for (var i = 0; i < keysLength; i++)
                {
                    var entry = unlockKeys->Get(i)->ToString();
                    if (keys.Contains(entry))
                    {
                        originalUnlocks[Array.IndexOf(keys, unlockKeys->Get(i)->ToString())] = rnsReloaded.utils.GetGlobalVar("otherUnlock")->Get(i)->ToLong();
                    }
                }
            }

            return returnValue;
        }

        // Unlock the keys in "keys" for the save file
        internal void UnlockSaveInfo()
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                unlockKeys = rnsReloaded.utils.GetGlobalVar("unlOtherKey");
                var keysLength = unlockKeys->ArrayLength();

                for (var i = 0; i < keysLength; i++)
                {
                    var entry = unlockKeys->Get(i)->ToString();
                    if (keys.Contains(entry))
                    {
                        *rnsReloaded.utils.GetGlobalVar("otherUnlock")->Get(i) = 1;
                    }
                }
            }
        }

        // Restore the keys to the original save file value
        internal void RestoreSaveInfo()
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var keysLength = unlockKeys->ArrayLength();

                for (var i = 0; i < keysLength; i++)
                {
                    var entry = unlockKeys->Get(i)->ToString();
                    if (keys.Contains(entry))
                    {
                        *rnsReloaded.utils.GetGlobalVar("otherUnlock")->Get(i) = originalUnlocks[i];
                    }
                }
            }
        }
    }
}
