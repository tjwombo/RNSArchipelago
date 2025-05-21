using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Xml.Linq;

namespace Rabit_and_Steel_Test
{
    public unsafe class Mod : IMod
    {
        static Random random = new Random();

        private WeakReference<IRNSReloaded>? rnsReloadedRef;
        private WeakReference<IReloadedHooks>? hooksRef;
        private ILoggerV1 logger = null!;

        private IHook<ScriptDelegate>? outskirtsHook;

        private IHook<ScriptDelegate>? setItemHook;

        private IHook<ScriptDelegate>? setCharHook;

        private IHook<ScriptDelegate>? inventoryHook;

        private IHook<ScriptDelegate>? selectCharacterAbilitiesHook;
        private List<int> availablePrimary = new List<int>();
        private List<int> availableSecondary = new List<int>();
        private List<int> availableSpecial = new List<int>();
        private List<int> availableDefensive = new List<int>();

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
                this.outskirtsHook.Activate();
                this.outskirtsHook.Enable();
                var outskirtsScriptN = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwaygen_outskirts_n") - 100000);
                this.outskirtsHook =
                    hooks.CreateHook<ScriptDelegate>(this.OutskirtsDetour, outskirtsScriptN->Functions->Function);
                this.outskirtsHook.Activate();
                this.outskirtsHook.Enable();


                var createItemId = rnsReloaded.ScriptFindId("scr_itemsys_create_item");
                var createItemScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.setItemHook = hooks.CreateHook<ScriptDelegate>(this.CreateTestItem, createItemScript->Functions->Function);
                this.setItemHook.Activate();
                this.setItemHook.Enable();

                //var charId = rnsReloaded.ScriptFindId("scr_runmenu_charinfo_return");
                //var charScript = rnsReloaded.GetScriptData(createItemId - 100000);
                //this.setCharHook = hooks.CreateHook<ScriptDelegate>(this.CharTest, charScript->Functions->Function);
                //this.setCharHook.Activate();
                //this.setCharHook.Enable();

                var inventoryId = rnsReloaded.ScriptFindId("scr_itemsys_populate_loot");
                var inventoryScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.inventoryHook = hooks.CreateHook<ScriptDelegate>(this.InventoryTest, inventoryScript->Functions->Function);
                this.inventoryHook.Activate();
                this.inventoryHook.Enable();

                var charId = rnsReloaded.ScriptFindId("scr_runmenu_charinfo_return");
                var charScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.selectCharacterAbilitiesHook = hooks.CreateHook<ScriptDelegate>(this.ChooseCharacterAbilities, charScript->Functions->Function);
                this.selectCharacterAbilitiesHook.Activate();
                this.selectCharacterAbilitiesHook.Enable();

                // Loot argv[2] == 1
                // Shop argv[2] == 2
                // Player argv[2] == 7

                // full heal argv[0] == 487
                // level up argv[0] == 488
                // regen potion argv[0] == 489
                // essence spell argv[0] == 490
                // darkness potion argv[0] == 491
                // quickening potion argv[0] == 492
                // winged potion argv[0] == 493
                // essence wit argv[0] == 494
                // swifthand potion argv[0] == 495
                // fire potion argv[0] == 496
                // strength potion argv[0] == 497
                // gold potion argv[0] == 498
                // luck potion argv[0] == 499
                // essence of steel argv[0] == 500
                // evasion potion argv[0] == 501
                // longarm potion argv[0] == 502
                // vitality potion argv[0] == 503

                // opal primary argv[0] == 504
                // opal secondary argv[0] == 505
                // opal special argv[0] == 506
                // opal defensive argv[0] == 507
                // sapphire argv[0] == 508-511
                // ruby argv[0] == 512-515
                // garnet argv[0] == 516-519
                // emerald argv[0] == 520-523


                //TODO: GET DATA FROM ARCHIPELAGO
                //TODO: Defender special does not work - needs another defender ability to charge, doesn't handle stored charges correct
                //TODO: Similarly, any defender ability adds a counter to you special that does nothing
                //TODO: Ancient abilities does not work as there is no friend
                availablePrimary.Add(6);
                availableSecondary.Add(13);
                availableSpecial.Add(20);
                availableDefensive.Add(27);
                availablePrimary.Add(34);
                availableSecondary.Add(41);
                availableSpecial.Add(48);
                availableDefensive.Add(55);
                availablePrimary.Add(62);
                availableSecondary.Add(69);
                availableSpecial.Add(76);
                availableDefensive.Add(83);
                availablePrimary.Add(90);
                availableSecondary.Add(97);
                availableSpecial.Add(104);
                availableDefensive.Add(111);
                availablePrimary.Add(118);
                availableSecondary.Add(125);
                availableSpecial.Add(132);
                availableDefensive.Add(139);
                availablePrimary.Add(146);
                availableSecondary.Add(153);
                availableSpecial.Add(160);
                availableDefensive.Add(167);
                availablePrimary.Add(174);
                availableSecondary.Add(181);
                availableSpecial.Add(188);
                availableDefensive.Add(195);
                availablePrimary.Add(202);
                availableSecondary.Add(209);
                availableSpecial.Add(216);
                availableDefensive.Add(223);
                availablePrimary.Add(230);
                availableSecondary.Add(237);
                availableSpecial.Add(244);
                availableDefensive.Add(251);
                availablePrimary.Add(258);
                availableSecondary.Add(265);
                availableSpecial.Add(272);
                availableDefensive.Add(279);


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
                new Notch(NotchType.IntroRoom, "", 0, 0),
                // Temp for testing because I'm too lazy to steel yourself lol
                new Notch(NotchType.Shop, "", 0, 0),
                new Notch(NotchType.Shop, "", 0, 0),
                new Notch(NotchType.Chest, "", 0, 0),
                new Notch(NotchType.Chest, "", 0, 0),
                new Notch(NotchType.Chest, "", 0, Notch.BOSS_FLAG),
                new Notch(NotchType.Boss, "enc_wolf_bluepaw0", 0, Notch.BOSS_FLAG)
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

        private RValue* ChooseCharacterAbilities(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("char", returnValue, argc, argv), Color.Gray);
            if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                int abilityId = (int)rnsReloaded.utils.RValueToLong(argv[0]);
                if (isPrimary(abilityId))
                {
                    *argv[0] = new RValue(availablePrimary[random.Next(availablePrimary.Count)]);
                } else if (isSecondary(abilityId))
                {
                    *argv[0] = new RValue(availableSecondary[random.Next(availableSecondary.Count)]);
                } else if (isSpecial(abilityId))
                {
                    *argv[0] = new RValue(availableSpecial[random.Next(availableSpecial.Count)]);
                } else if (isDefensive(abilityId))
                {
                    *argv[0] = new RValue(availableDefensive[random.Next(availableDefensive.Count)]);
                }
            }
            returnValue = this.selectCharacterAbilitiesHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        private bool isPrimary(int abilityId) 
        {
            return availablePrimary.Contains(abilityId);
        }

        private bool isSecondary(int abilityId)
        {
            return availableSecondary.Contains(abilityId);
        }

        private bool isSpecial(int abilityId)
        {
            return availableSpecial.Contains(abilityId);
        }

        private bool isDefensive(int abilityId)
        {
            return availableDefensive.Contains(abilityId);
        }

        private bool isWizard(int abilityId)
        {
            return abilityId == 6 || abilityId == 13 || abilityId == 20 || abilityId == 27;
        }

        private bool isAssassin(int abilityId)
        {
            return abilityId == 34 || abilityId == 41 || abilityId == 48 || abilityId == 55;
        }

        private bool isHeavyBlade(int abilityId)
        {
            return abilityId == 62 || abilityId == 69 || abilityId == 76 || abilityId == 83;
        }

        private bool isDancer(int abilityId)
        {
            return abilityId == 90 || abilityId == 97 || abilityId == 104 || abilityId == 111;
        }

        private bool isDruid(int abilityId)
        {
            return abilityId == 118 || abilityId == 125 || abilityId == 132 || abilityId == 139;
        }

        private bool isSpellsword(int abilityId)
        {
            return abilityId == 146 || abilityId == 153 || abilityId == 160 || abilityId == 167;
        }

        private bool isSniper(int abilityId)
        {
            return abilityId == 174 || abilityId == 181 || abilityId == 188 || abilityId == 195;
        }

        private bool isBruiser(int abilityId)
        {
            return abilityId == 202 || abilityId == 209 || abilityId == 216 || abilityId == 223;
        }

        private bool isDefender(int abilityId)
        {
            return abilityId == 230 || abilityId == 237 || abilityId == 244 || abilityId == 251;
        }

        private bool isAncient(int abilityId)
        {
            return abilityId == 258 || abilityId == 265 || abilityId == 272 || abilityId == 279;
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
