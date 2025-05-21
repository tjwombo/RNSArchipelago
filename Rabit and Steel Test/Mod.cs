/*
using Rabit_and_Steel_Test.Configuration;
using Rabit_and_Steel_Test.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace Rabit_and_Steel_Test
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;


            // For more information about this template, please see
            // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

            // If you want to implement e.g. unload support in your mod,
            // and some other neat features, override the methods in ModBase.

            // TODO: Implement some mod logic
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}
*/

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Xml.Linq;

namespace Rabit_and_Steel_Test
{
    public unsafe class Mod : IMod
    {
        private WeakReference<IRNSReloaded>? rnsReloadedRef;
        private WeakReference<IReloadedHooks>? hooksRef;
        private ILoggerV1 logger = null!;

        private IHook<ScriptDelegate>? outskirtsHook;

        private IHook<ScriptDelegate>? setItemHook;

        private IHook<ScriptDelegate>? setCharHook;

        private IHook<ScriptDelegate>? inventoryHook;

        public void Start(IModLoaderV1 loader)
        {
            this.rnsReloadedRef = loader.GetController<IRNSReloaded>();
            this.hooksRef = loader.GetController<IReloadedHooks>()!;

            this.logger = loader.GetLogger();

            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                rnsReloaded.OnReady += this.Ready;
            }
        }

        public void Ready()
        {
            if (
                this.rnsReloadedRef != null
                && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
                && this.hooksRef != null
                && this.hooksRef.TryGetTarget(out var hooks)
            )
            {
                
                var encounterId = rnsReloaded.ScriptFindId("scr_enemy_add_pattern");
                var encounterScript = rnsReloaded.GetScriptData(encounterId - 100000);

                var outskirtsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwaygen_outskirts") - 100000);
                this.outskirtsHook =
                    hooks.CreateHook<ScriptDelegate>(this.OutskirtsDetour, outskirtsScript->Functions->Function);
                //this.outskirtsHook.Activate();
                //this.outskirtsHook.Enable();


                var createItemId = rnsReloaded.ScriptFindId("scr_itemsys_create_item");
                var createItemScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.setItemHook = hooks.CreateHook<ScriptDelegate>(this.CreateTestItem, createItemScript->Functions->Function);
                this.setItemHook.Activate();
                this.setItemHook.Enable();

                var charId = rnsReloaded.ScriptFindId("scr_runmenu_charinfo_return");
                var charScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.setCharHook = hooks.CreateHook<ScriptDelegate>(this.CharTest, charScript->Functions->Function);
                this.setCharHook.Activate();
                this.setCharHook.Enable();

                var inventoryId = rnsReloaded.ScriptFindId("scr_itemsys_populate_loot");
                var inventoryScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.inventoryHook = hooks.CreateHook<ScriptDelegate>(this.InventoryTest, inventoryScript->Functions->Function);
                this.inventoryHook.Activate();
                this.inventoryHook.Enable();

            }
        }

        private bool IsReady(
            [MaybeNullWhen(false), NotNullWhen(true)] out IRNSReloaded rnsReloaded
        )
        {
            if (this.rnsReloadedRef != null)
            {
                this.rnsReloadedRef.TryGetTarget(out var rnsReloadedRef);
                rnsReloaded = rnsReloadedRef;
                return rnsReloaded != null;
            }
            rnsReloaded = null;
            return false;
        }

        private RValue* OutskirtsDetour(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.outskirtsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (this.IsReady(out var rnsReloaded))
            {
                rnsReloaded.utils.setHallway(new List<Notch> {
                //new Notch(NotchType.IntroRoom, "", 0, 0),
                // Temp for testing because I'm too lazy to steel yourself lol
                new Notch(NotchType.Chest, "", 0, 0),
                new Notch(NotchType.Chest, "", 0, 0),
                new Notch(NotchType.Chest, "", 0, 0),
                new Notch(NotchType.Chest, "", 0, 0),
                new Notch(NotchType.Chest, "", 0, Notch.BOSS_FLAG),
                //new Notch(NotchType.Boss, "enc_wolf_bluepaw0", 0, Notch.BOSS_FLAG)
            }, self, rnsReloaded);
            }
            return returnValue;
        }

        private string PrintHook(string name, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (argc == 0)
                {
                    return $"{name}() -> {rnsReloaded.GetString(returnValue)}";
                }
                else
                {
                    var args = new List<string>();
                    for (var i = 0; i < argc; i++)
                    {
                        args.Add(rnsReloaded.GetString(argv[i]));
                    }

                    return $"{name}({string.Join(", ", args)}) -> {rnsReloaded.GetString(returnValue)}";
                }
            }
            else
            {
                return string.Empty;
            }
        }


        private RValue* CreateTestItem(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("item", returnValue, argc, argv), Color.Gray);
            returnValue = this.setItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        
        private RValue* CharTest(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("char", returnValue, argc, argv), Color.Gray);
            // Original function seems to attach the character abilities
            returnValue = this.setCharHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        private RValue* InventoryTest(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("inventory", returnValue, argc, argv), Color.Gray);
            returnValue = this.inventoryHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        public void Suspend()
        {
            //this.addPatternHook?.Disable();
            //this.bossHealHook?.Disable();
        }

        public void Resume()
        {
            //this.addPatternHook?.Enable();
            //this.bossHealHook?.Enable();
            // once ready, set cansuspend to true
        }

        public bool CanSuspend() => false;

        public void Unload() { }
        public bool CanUnload() => false;

        public Action Disposing => () => { };


    }
}
