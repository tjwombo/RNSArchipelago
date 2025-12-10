using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RnSArchipelago.Game
{
    internal unsafe class ShopHandler
    {
        private readonly IRNSReloaded rnsReloaded;
        private readonly ILoggerV1 logger;

        internal IHook<ScriptDelegate>? setItemHook;

        internal ShopHandler(IRNSReloaded rnsReloaded, ILoggerV1 logger)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
        }

        internal RValue* SetItem(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.setItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
            this.logger.PrintMessage(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 2)->ToString(), System.Drawing.Color.Red);
            this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "shop", self, returnValue, argc, argv), System.Drawing.Color.Red);
            return returnValue;
        }
    }
}
