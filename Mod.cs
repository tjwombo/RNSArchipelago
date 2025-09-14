using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Utils;
using RNSReloaded;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace RnSArchipelago
{
    public unsafe class Mod : IMod
    {
        static Random random = new Random();

        private WeakReference<IRNSReloaded>? rnsReloadedRef;
        private WeakReference<IReloadedHooks>? hooksRef;
        private ILoggerV1 logger = null!;

        private IHook<ScriptDelegate>? roomChangeHook;

        private IHook<ScriptDelegate>? outskirtsHook;

        private IHook<ScriptDelegate>? setItemHook;

        private IHook<ScriptDelegate>? setCharHook;

        private IHook<ScriptDelegate>? inventoryHook;

        private IHook<ScriptDelegate>? endGameHook;


        private IHook<ScriptDelegate>? archipelagoWebsocketHook;

        private IHook<ScriptDelegate>? selectCharacterAbilitiesHook;
        private List<int> availablePrimary = new List<int>();
        private List<int> availableSecondary = new List<int>();
        private List<int> availableSpecial = new List<int>();
        private List<int> availableDefensive = new List<int>();

        LobbySettings lobby;
        ArchipelagoConnection conn;

        public void Start(IModLoaderV1 loader)
        {
            this.rnsReloadedRef = loader.GetController<IRNSReloaded>();
            this.hooksRef = loader.GetController<IReloadedHooks>();

            this.logger = loader.GetLogger();

            if (this.IsReady(out var rnsReloaded))
            {
                rnsReloaded.OnReady += this.Ready;
            }
        }

        public void Ready()
        {
            if (
                this.IsReady(out var rnsReloaded)
                && this.hooksRef != null
                && this.hooksRef.TryGetTarget(out var hooks)
            )
            {
                //var encounterId = rnsReloaded.ScriptFindId("scr_enemy_add_pattern"); // unsure what this was for, probably just to have for later use
                //var encounterScript = rnsReloaded.GetScriptData(encounterId - 100000);

                lobby = new LobbySettings(rnsReloaded, logger, hooks);
                conn = new ArchipelagoConnection(rnsReloaded, logger, hooks);

                /*var createItemId = rnsReloaded.ScriptFindId("scr_itemsys_create_item"); // unsure what this was for, probably just to see item names and their ids
                var createItemScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.setItemHook = hooks.CreateHook<ScriptDelegate>(this.CreateTestItem, createItemScript->Functions->Function);
                this.setItemHook.Activate();
                this.setItemHook.Enable();*/

                //var charId = rnsReloaded.ScriptFindId("scr_runmenu_charinfo_return"); // unsure what this was for, probably just to see player abiliies and their ids
                //var charScript = rnsReloaded.GetScriptData(createItemId - 100000);
                //this.setCharHook = hooks.CreateHook<ScriptDelegate>(this.CharTest, charScript->Functions->Function);
                //this.setCharHook.Activate();
                //this.setCharHook.Enable();

                /*var inventoryId = rnsReloaded.ScriptFindId("scr_itemsys_populate_loot"); // unsure what this was for, probably to force items in chests
                var inventoryScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.inventoryHook = hooks.CreateHook<ScriptDelegate>(this.InventoryTest, inventoryScript->Functions->Function);
                this.inventoryHook.Activate();
                this.inventoryHook.Enable();*/


                AddArchipelagoButtonToMenu(); // Adds archipelago as a lobbyType
                AddArchipelagoOptionsToMenu(); // Adds the options for archipelago



                SetupArchipelagoWebsocket(); // Creates the websocket for archipelago



                //RandomizePlayerAbilities(rnsReloaded, hooks); // randomize the player abilities
 

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

        // Set up the hooks relating to archipelago options
        private void AddArchipelagoButtonToMenu()
        {
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {
                var menuId = rnsReloaded.ScriptFindId("scr_runmenu_make_lobby_select");
                var menuScript = rnsReloaded.GetScriptData(menuId - 100000);
                lobby.archipelagoButtonHook = hooks.CreateHook<ScriptDelegate>(lobby.CreateArchipelagoLobbyType, menuScript->Functions->Function);
                lobby.archipelagoButtonHook.Activate();
                lobby.archipelagoButtonHook.Enable();

            }
        }

        // Set up the hooks relating to archipelago options
        private void AddArchipelagoOptionsToMenu()
        {
            if (
                this.IsReady(out var rnsReloaded, out var hooks)
            )
            {
                var optionsId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_setup");
                var optionsScript = rnsReloaded.GetScriptData(optionsId - 100000);
                lobby.archipelagoOptionsHook = hooks.CreateHook<ScriptDelegate>(lobby.CreateArchipelagoOptions, optionsScript->Functions->Function);
                lobby.archipelagoOptionsHook.Activate();
                lobby.archipelagoOptionsHook.Enable();

                // Create a sudo step function to run while the display is visible
                var osId = rnsReloaded.CodeFunctionFind("os_get_info");
                if (osId.HasValue)
                {
                    var osScript = rnsReloaded.GetScriptData(osId.Value);
                    this.logger.PrintMessage("" + (osScript != null), Color.Red);
                    lobby.lobbySettingsDisplayStepHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsDisplayStep, osScript->Functions->Function);
                    lobby.lobbySettingsDisplayStepHook.Activate();
                }

                var nameId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_name");
                var nameScript = rnsReloaded.GetScriptData(nameId - 100000);
                lobby.setNameHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsName, nameScript->Functions->Function);
                lobby.setNameHook.Activate();
                lobby.setNameHook.Enable();

                var descId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_desc");
                var descScript = rnsReloaded.GetScriptData(descId - 100000);
                lobby.setDescHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsDesc, descScript->Functions->Function);
                lobby.setDescHook.Activate();
                lobby.setDescHook.Enable();

                var passId = rnsReloaded.ScriptFindId("textboxcomp_set_password");
                var passScript = rnsReloaded.GetScriptData(passId - 100000);
                lobby.setPassHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsPass, passScript->Functions->Function);
                lobby.setPassHook.Activate();
                lobby.setPassHook.Enable();

                var returnId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_return");
                var returnScript = rnsReloaded.GetScriptData(returnId - 100000);
                lobby.archipelagoOptionsReturnHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettings, returnScript->Functions->Function);
                lobby.archipelagoOptionsReturnHook.Activate();
                lobby.archipelagoOptionsReturnHook.Enable();

                var titleId = rnsReloaded.ScriptFindId("scr_runmenu_char_return");
                var titleScript = rnsReloaded.GetScriptData(titleId - 100000);
                lobby.lobbyTitleHook = hooks.CreateHook<ScriptDelegate>(lobby.LobbyToTitle, titleScript->Functions->Function);
                lobby.lobbyTitleHook.Activate();
                lobby.lobbyTitleHook.Enable();

            }
        }


        // Create the hooks to set up the websocket
        private void SetupArchipelagoWebsocket()
        {
            if (
                this.IsReady(out var rnsReloaded, out var hooks)
            )
            {
                var menuId = rnsReloaded.ScriptFindId("scr_runmenu_main_startrun_multi");
                var menuScript = rnsReloaded.GetScriptData(menuId - 100000);
                this.archipelagoWebsocketHook = hooks.CreateHook<ScriptDelegate>(this.CreateArchipelagoWebsocket, menuScript->Functions->Function);
                this.archipelagoWebsocketHook.Activate();
                this.archipelagoWebsocketHook.Enable();

                var messageId = rnsReloaded.ScriptFindId("scr_chat_add_message");
                var messageScript = rnsReloaded.GetScriptData(messageId - 100000);
                this.setItemHook = hooks.CreateHook<ScriptDelegate>(this.test, messageScript->Functions->Function);
                this.setItemHook.Activate();
                this.setItemHook.Enable();
            }
        }

        private RValue* test(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (
                this.IsReady(out var rnsReloaded, out var hooks)
            )
            {
                this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "message", returnValue, argc, argv), Color.Gray);
                
            }
            returnValue = this.setItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Create and validate the websocket connection to archipelago before moving to the character selection room
        private RValue* CreateArchipelagoWebsocket(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (this.IsReady(out var rnsReloaded))
            {
                // If the lobby type is archipelago set up the websocket
                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                {
                    // Validate archipelago options / connection
                    ArchipelagoConfig config = ArchipelagoConfig.Instance;
                    config.setConfig(lobby.ArchipelagoName, lobby.ArchipelagoAddress, lobby.ArchipelagoNum, lobby.ArchipelagoPassword);
                    conn.StartConnection();


                    // Show error and return if connection problem


                    // Setup as if a friends only lobby or solo lobby based on the number of players
                    this.logger.PrintMessage("" + lobby.ArchipelagoNum, Color.Red);
                    if (lobby.ArchipelagoNum > 1)
                    {
                        *rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(1);
                    } else
                    {
                        *rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(0);
                    }
                    returnValue = this.archipelagoWebsocketHook!.OriginalFunction(self, other, returnValue, argc, argv);

                    // Return to archipelago lobby
                    *rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(3);
                } else
                {
                    // Otherwise continue normally
                    returnValue = this.archipelagoWebsocketHook!.OriginalFunction(self, other, returnValue, argc, argv);
                }
                
            }
           
            return returnValue;
        }

        // Changes the outskirts routing to only have shots and chests besides the boss
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

        

        private RValue* CreateTestItem(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            //this.logger.PrintMessage(this.PrintHook("item", returnValue, argc, argv), Color.Gray);
            returnValue = this.setItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        
        private RValue* CharTest(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            //this.logger.PrintMessage(this.PrintHook("char", returnValue, argc, argv), Color.Gray);
            // Original function seems to attach the character abilities
            returnValue = this.setCharHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        private RValue* InventoryTest(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
           // this.logger.PrintMessage(this.PrintHook("inventory", returnValue, argc, argv), Color.Gray);
            returnValue = this.inventoryHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Sets up the hooks to randomize the player abiliteis
        private void RandomizePlayerAbilities()
        {
            if (
                this.IsReady(out var rnsReloaded)
                && this.hooksRef != null
                && this.hooksRef.TryGetTarget(out var hooks)
            )
            {
                var createItemId = rnsReloaded.ScriptFindId("scr_itemsys_create_item");
                var createItemScript = rnsReloaded.GetScriptData(createItemId - 100000);
                this.selectCharacterAbilitiesHook = hooks.CreateHook<ScriptDelegate>(this.ChooseCharacterAbilities, createItemScript->Functions->Function);
                this.selectCharacterAbilitiesHook.Activate();
                this.selectCharacterAbilitiesHook.Enable();
            }
        }

        // Randomly assign an ability of the same type from the available list of abilities
        private RValue* ChooseCharacterAbilities(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (this.IsReady(out var rnsReloaded))
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

        // Helper function to check if the weakreference are ready and get the strong reference
        private bool IsReady(
            [MaybeNullWhen(false), NotNullWhen(true)] out IRNSReloaded rnsReloaded
        )
        {
            if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out rnsReloaded))
            {
                return rnsReloaded != null;
            }
            rnsReloaded = null;
            return false;
        }

        private bool IsReady(
            [MaybeNullWhen(false), NotNullWhen(true)] out IRNSReloaded rnsReloaded,
            [MaybeNullWhen(false), NotNullWhen(true)] out IReloadedHooks hooks
        )
        {
            if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out hooks)
        )
            {
                return rnsReloaded != null;
            }
            rnsReloaded = null;
            hooks = null;
            return false;
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
