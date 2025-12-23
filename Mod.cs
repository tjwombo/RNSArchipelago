using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using RnSArchipelago.Utils;
using RnSArchipelago.Config;
using RnSArchipelago.Connection;
using RnSArchipelago.Data;
using RnSArchipelago.Game;

namespace RnSArchipelago
{
    public unsafe class Mod : IMod
    {
        static Random random = new Random();

        private WeakReference<IRNSReloaded>? rnsReloadedRef;
        private WeakReference<IReloadedHooks>? hooksRef;
        private ILoggerV1 logger = null!;

        private Configurator configurator = null!;
        private Config.Config config = null!;
        private KingdomHandler kingdom = null!;
        private ClassHandler classHandler = null!;
        private LocationHandler locationHandler = null!;

        private readonly SharedData data = new();

        private IHook<ScriptDelegate>? roomChangeHook;

        private IHook<ScriptDelegate>? outskirtsHook;
        private IHook<ScriptDelegate>? outskirtsNHook;

        private IHook<ScriptDelegate>? setItemHook;

        private IHook<ScriptDelegate>? setCharHook;

        private IHook<ScriptDelegate>? inventoryHook;

        private IHook<ScriptDelegate>? endGameHook;

        private IHook<ScriptDelegate>? archipelagoWebsocketHook;

        private IHook<ScriptDelegate>? selectCharacterAbilitiesHook;
        private readonly List<int> availablePrimary = [];
        private readonly List<int> availableSecondary = [];
        private readonly List<int> availableSpecial = [];
        private readonly List<int> availableDefensive = [];

        internal IHook<ScriptDelegate>? oneShotHook;

        private LobbySettings? lobby;
        private ArchipelagoConnection? conn;

        public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig)
        {
            this.rnsReloadedRef = loader.GetController<IRNSReloaded>();
            this.hooksRef = loader.GetController<IReloadedHooks>();

            this.logger = loader.GetLogger();

            this.configurator = new Configurator(((IModLoader)loader).GetModConfigDirectory(modConfig.ModId));
            this.config = this.configurator.GetConfiguration<Config.Config>(0);
            this.config.ConfigurationUpdated += this.ConfigurationUpdated;

            if (!this.config.SkipItemCreation)
            {
                CopyItemModToRnSMod();
            }

            if (this.IsReady(out var rnsReloaded))
            {
                rnsReloaded.OnReady += this.Ready;
            }
        }

        private void ConfigurationUpdated(IUpdatableConfigurable newConfig)
        {
            this.config = (Config.Config)newConfig;
        }

        public void Ready()
        {
            if (
                this.IsReady(out var rnsReloaded)
                && this.hooksRef != null
                && this.hooksRef.TryGetTarget(out var hooks)
            )
            {
                locationHandler = new LocationHandler(rnsReloaded, logger);
                conn = new ArchipelagoConnection(rnsReloaded, logger, this.config, data, locationHandler);
                lobby = new LobbySettings(rnsReloaded, logger, hooks, data);
                kingdom = new KingdomHandler(rnsReloaded, logger);
                classHandler = new ClassHandler(rnsReloaded, logger);

                //TODO:  TEMP FOR QUICK ACCESS TO SHOP FOR TESTING
                /*var outskirtsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwaygen_outskirts") - 100000);
                this.outskirtsHook =
                    hooks.CreateHook<ScriptDelegate>(this.OutskirtsDetour, outskirtsScript->Functions->Function);
                this.outskirtsHook.Activate();
                this.outskirtsHook.Enable();

                var outskirtsScriptN = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwaygen_outskirts_n") - 100000);
                this.outskirtsNHook =
                    hooks.CreateHook<ScriptDelegate>(this.OutskirtsDetour, outskirtsScriptN->Functions->Function);
                this.outskirtsNHook.Activate();
                this.outskirtsNHook.Enable();*/

                // Test temp hook
                /*var testId = rnsReloaded.ScriptFindId("scr_itemsys_draw_item");
                var testScript = rnsReloaded.GetScriptData(testId - 100000);
                this.setItemHook = hooks.CreateHook<ScriptDelegate>(this.test, testScript->Functions->Function);
                this.setItemHook.Activate();
                this.setItemHook.Enable();*/


                AddArchipelagoButtonToMenu(); // Adds archipelago as a lobbyType
                AddArchipelagoOptionsToMenu(); // Adds the options for archipelago

                SetupArchipelagoWebsocket(); // Creates the websocket for archipelago

                SetupSendBattleAndChestLocations(); // Creates the hook to send locations on encounter win and chest open
                SetupArchipelagoItems(); // Creates the archipelago items and puts them in the correct chest

                SetupKingdomSanity(); // Modifies the route based on current items

                SetupClassSanity(); // Limits the classes you can play based on current items

                // TODO: REMOVE ONCE DONE TESTING
                //oneShot();

                // TODO: IMPLEMENT RANDOMIZATION OPTION AND GET THE UPGRADES WORKING FOR BASE RANDOM ABILITY
                //RandomizePlayerAbilities(); // randomize the player abilities


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
                lobby!.archipelagoButtonHook = hooks.CreateHook<ScriptDelegate>(lobby.CreateArchipelagoLobbyType, menuScript->Functions->Function);
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
                // Create the archipelago lobby type
                var optionsId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_setup");
                var optionsScript = rnsReloaded.GetScriptData(optionsId - 100000);
                lobby!.archipelagoOptionsHook = hooks.CreateHook<ScriptDelegate>(lobby.CreateArchipelagoOptions, optionsScript->Functions->Function);
                lobby.archipelagoOptionsHook.Activate();
                lobby.archipelagoOptionsHook.Enable();

                // Create a sudo step function to run while the display is visible
                var osId = rnsReloaded.CodeFunctionFind("os_get_info");
                if (osId.HasValue)
                {
                    // Update the info banner
                    var osScript = rnsReloaded.GetScriptData(osId.Value);
                    this.logger.PrintMessage("" + (osScript != null), Color.Red);
                    lobby.lobbySettingsDisplayStepHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsDisplayStep, osScript->Functions->Function);
                    lobby.lobbySettingsDisplayStepHook.Activate();
                }

                // Update the name if entered before returning
                var nameId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_name");
                var nameScript = rnsReloaded.GetScriptData(nameId - 100000);
                lobby.setNameHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsName, nameScript->Functions->Function);
                lobby.setNameHook.Activate();
                lobby.setNameHook.Enable();

                // Update the description if entered before returning
                var descId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_desc");
                var descScript = rnsReloaded.GetScriptData(descId - 100000);
                lobby.setDescHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsDesc, descScript->Functions->Function);
                lobby.setDescHook.Activate();
                lobby.setDescHook.Enable();

                // Update the password if entered before returning
                var passId = rnsReloaded.ScriptFindId("textboxcomp_set_password");
                var passScript = rnsReloaded.GetScriptData(passId - 100000);
                lobby.setPassHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsPass, passScript->Functions->Function);
                lobby.setPassHook.Activate();
                lobby.setPassHook.Enable();

                // Update all the settings after returning
                var returnId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_return");
                var returnScript = rnsReloaded.GetScriptData(returnId - 100000);
                lobby.archipelagoOptionsReturnHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettings, returnScript->Functions->Function);
                lobby.archipelagoOptionsReturnHook.Activate();
                lobby.archipelagoOptionsReturnHook.Enable();

                // TODO: PROBABLY DOESN'T FIRE IF GAME CRASHES AND THAT CAUSES ARCHIPELAGO SETTINGS TO PERSIST IN REGULAR
                // Restore settings to original when returned to the main menu
                var titleId = rnsReloaded.ScriptFindId("scr_runmenu_char_return");
                var titleScript = rnsReloaded.GetScriptData(titleId - 100000);
                lobby.lobbyTitleHook = hooks.CreateHook<ScriptDelegate>(lobby.LobbyToTitle, titleScript->Functions->Function);
                lobby.lobbyTitleHook.Activate();
                lobby.lobbyTitleHook.Enable();
            }
        }

        // Set up the hooks to set up the websocket
        private void SetupArchipelagoWebsocket()
        {
            if (
                this.IsReady(out var rnsReloaded, out var hooks)
            )
            {
                // Start the connection to archipelago
                var menuId = rnsReloaded.ScriptFindId("scr_runmenu_main_startrun_multi");
                var menuScript = rnsReloaded.GetScriptData(menuId - 100000);
                this.archipelagoWebsocketHook = hooks.CreateHook<ScriptDelegate>(this.CreateArchipelagoWebsocket, menuScript->Functions->Function);
                this.archipelagoWebsocketHook.Activate();
                this.archipelagoWebsocketHook.Enable();

                // Close the connection after returning to lobby settings
                var resetId = rnsReloaded.ScriptFindId("scr_runmenu_disband_disband");
                var resetScript = rnsReloaded.GetScriptData(resetId - 100000);
                conn!.resetConnHook = hooks.CreateHook<ScriptDelegate>(conn.ResetConn, resetScript->Functions->Function);
                conn.resetConnHook.Activate();
                conn.resetConnHook.Enable();

                // Close the connection after returning to lobby settings from a victory/defeat screen
                var resetEndId = rnsReloaded.ScriptFindId("scr_runmenu_victory_disbandlobby");
                var resetEndScript = rnsReloaded.GetScriptData(resetEndId - 100000);
                conn.resetConnEndHook = hooks.CreateHook<ScriptDelegate>(conn.ResetConnEnd, resetEndScript->Functions->Function);
                conn.resetConnEndHook.Activate();
                conn.resetConnEndHook.Enable();
            }
        }

        // Set up the hooks to send locations through the websocket
        private void SetupSendBattleAndChestLocations()
        {
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {
                // Send out locations on encounter win
                var battleWonScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_notchexbattle_victory_transfer") - 100000);
                locationHandler.notchCompleteHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SendNotchComplete, battleWonScript->Functions->Function);
                locationHandler.notchCompleteHook.Activate();
                locationHandler.notchCompleteHook.Enable();

                // Send out locations on chest open
                var chestOpenScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_npc_treasure_explode") - 100000);
                locationHandler.chestOpenHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SendChestOpen, chestOpenScript->Functions->Function);
                locationHandler.chestOpenHook.Activate();
                locationHandler.chestOpenHook.Enable();
            }
        }

        // Set up the hooks to manipulate chest items
        private void SetupArchipelagoItems()
        {
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {

                // Setup the mod items that corresponds to the items in CopyItemModToRnSMod()
                var setupItemsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_init_mods") - 100000);
                locationHandler.setupItemsHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetupArchipelagoItems, setupItemsScript->Functions->Function);
                locationHandler.setupItemsHook.Activate();
                locationHandler.setupItemsHook.Enable();

                // Make it so that the archipelago items mod cannot be disabled
                var modApplyScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_runmenu_mods_apply") - 100000);
                locationHandler.enableModHook = hooks.CreateHook<ScriptDelegate>(locationHandler.EnableArchipelagoItems, modApplyScript->Functions->Function);
                locationHandler.enableModHook.Activate();
                locationHandler.enableModHook.Enable();

                // Set amount of items in chest, 5 for archipelago, normal for chest # for normal
                var itemAmtScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_loot_amount") - 100000);
                locationHandler.itemAmtHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetAmountOfItems, itemAmtScript->Functions->Function);
                locationHandler.itemAmtHook.Activate();
                locationHandler.itemAmtHook.Enable();

                // Get the id of the archipelago item 
                var itemGetScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_readsheet_items") - 100000);
                locationHandler.itemGetHook = hooks.CreateHook<ScriptDelegate>(locationHandler.GetItems, itemGetScript->Functions->Function);
                locationHandler.itemGetHook.Activate();
                locationHandler.itemGetHook.Enable();

                // Scout the archipelago item to display values in a chest
                var itemScoutChestScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_populate_loot") - 100000);
                locationHandler.itemScoutChestHook = hooks.CreateHook<ScriptDelegate>(locationHandler.ScoutChestItems, itemScoutChestScript->Functions->Function);
                locationHandler.itemScoutChestHook.Activate();
                locationHandler.itemScoutChestHook.Enable();

                // Scout the archipelago item to display values in a shop
                var itemScoutShopScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_populate_store") - 100000);
                locationHandler.itemScoutShopHook = hooks.CreateHook<ScriptDelegate>(locationHandler.ScoutShopItems, itemScoutShopScript->Functions->Function);
                locationHandler.itemScoutShopHook.Activate();
                locationHandler.itemScoutShopHook.Enable();

                // Set the item to be an archipelago item
                var itemSetScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_create_item") - 100000);
                locationHandler.itemSetHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetItems, itemSetScript->Functions->Function);
                locationHandler.itemSetHook.Activate();
                locationHandler.itemSetHook.Enable();

                // Set the description of an item to match its archipelago item
                var itemSetDescriptionScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_infodraw_get_item_desc") - 100000);
                locationHandler.itemSetDescriptionHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetItemsDescription, itemSetDescriptionScript->Functions->Function);
                locationHandler.itemSetDescriptionHook.Activate();
                locationHandler.itemSetDescriptionHook.Enable();

                //TODO: FINISH GETTING IT WORKING
                // Set the shop upgrade description to match the class that ability is from
                /*var itemSetUpgradeDescriptionId = rnsReloaded.ScriptFindId("scr_infodraw_get_item_desc");
                var itemSetUpgradeDescriptionScript = rnsReloaded.GetScriptData(itemSetUpgradeDescriptionId - 100000);
                locationHandler.itemSetUpgradeDescriptionHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetUpgradeDescription, itemSetUpgradeDescriptionScript->Functions->Function);
                locationHandler.itemSetUpgradeDescriptionHook.Activate();
                locationHandler.itemSetUpgradeDescriptionHook.Enable();*/

                // Prevents you from actually taking an item, and sends out the corresponding location
                var takeItemScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_pickup_loot") - 100000);
                locationHandler.takeItemHook = hooks.CreateHook<ScriptDelegate>(locationHandler.TakeItem, takeItemScript->Functions->Function);
                locationHandler.takeItemHook.Activate();
                locationHandler.takeItemHook.Enable();

                // Give treasurespheres that have accumulated 
                var treasuresphereOnStartNScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_generate") - 100000);
                locationHandler.spawnTreasuresphereOnStartNHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SpawnTreasuresphereOnStart, treasuresphereOnStartNScript->Functions->Function);
                locationHandler.spawnTreasuresphereOnStartNHook.Activate();
                locationHandler.spawnTreasuresphereOnStartNHook.Enable();
            }
        }

        // Set up the hooks for class sanity handling
        private void SetupClassSanity()
        {
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {
                // Visually lock characters not yet obtained
                var lockVisualClassScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_charselect2_update_chooseclass") - 100000);
                classHandler.lockVisualClassHook = hooks.CreateHook<ScriptDelegate>(classHandler.LockVisualClass, lockVisualClassScript->Functions->Function);
                classHandler.lockVisualClassHook.Activate();
                classHandler.lockVisualClassHook.Enable();

                // Prevent the drawing of colors if trying to select a locked class
                var stopColorScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_charselect2_setup_colors") - 100000);
                classHandler.stopColorHook = hooks.CreateHook<ScriptDelegate>(classHandler.StopColorDraw, stopColorScript->Functions->Function);
                classHandler.stopColorHook.Activate();
                classHandler.stopColorHook.Enable();

                // Actually lock characters not yet obtained
                var lockClassScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_charselect2_update_choosecolor") - 100000);
                classHandler.lockClassHook = hooks.CreateHook<ScriptDelegate>(classHandler.LockClass, lockClassScript->Functions->Function);
                classHandler.lockClassHook.Activate();
                classHandler.lockClassHook.Enable();
            }
        }

        // TODO: REMOVE Testing function for timing printing
        private RValue* test(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (
                this.IsReady(out var rnsReloaded, out var hooks)
            )
            {
                returnValue = this.setItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
                //this.logger.PrintMessage(new RValue(self).ToString(), Color.Red);
                //this.logger.PrintMessage(HookUtil.FindLayerWithField(rnsReloaded, "displayStr"), Color.Red);
                this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "test", self, returnValue, argc, argv), Color.Gray);
                return returnValue;
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
                if (HookUtil.IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                {
                    // Validate archipelago options / connection
                    this.data.SetValue<string>(DataContext.Connection, "name", lobby!.ArchipelagoName);
                    this.data.SetValue<string>(DataContext.Connection, "address", lobby.ArchipelagoAddress);
                    this.data.SetValue<string>(DataContext.Connection, "numPlayers", ""+lobby.ArchipelagoNum);
                    this.data.SetValue<string>(DataContext.Connection, "password", lobby.ArchipelagoPassword);
                    conn!.StartConnection();

                    // Tell the inventory utils that we are in archipelago mode
                    //InventoryUtil.Instance.isActive = true;

                    // TODO: Show error and return if connection problem


                    // Setup as if a friends only lobby or solo lobby based on the number of players
                    this.logger.PrintMessage("" + lobby.ArchipelagoNum, Color.Red);
                    if (lobby.ArchipelagoNum > 1)
                    {
                        *rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(1);
                    } else
                    {
                        // TODO: Fix this as there was errors stopping a single player lobby
                        //*rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(0);
                        *rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(1);
                    }
                    returnValue = this.archipelagoWebsocketHook!.OriginalFunction(self, other, returnValue, argc, argv);

                    // Return to archipelago lobby
                    *rnsReloaded.utils.GetGlobalVar("obLobbyType") = new RValue(3);
                } else
                {
                    // Tell the inventory utils that we are in normal mode
                    //InventoryUtil.Instance.isActive = false;

                    // Otherwise continue normally
                    returnValue = this.archipelagoWebsocketHook!.OriginalFunction(self, other, returnValue, argc, argv);
                }
            }
           
            return returnValue;
        }

        // Set up the hooks for kingdom sanity handling
        private void SetupKingdomSanity()
        {
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {
                // Create the planned route
                var chooseHallsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_choose_halls") - 100000);
                kingdom.chooseHallsHook = hooks.CreateHook<ScriptDelegate>(kingdom.CreateRoute, chooseHallsScript->Functions->Function);
                kingdom.chooseHallsHook.Activate();
                kingdom.chooseHallsHook.Enable();

                // Modify the icons on the route selection screen
                var iconsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_stagefirst_available") - 100000);
                kingdom.fixChooseIconsHook = hooks.CreateHook<ScriptDelegate>(kingdom.ModifyRouteIcons, iconsScript->Functions->Function);
                kingdom.fixChooseIconsHook.Activate();
                kingdom.fixChooseIconsHook.Enable();

                // Make sure you can go to the next hallway
                var endHallsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_move_next") - 100000);
                kingdom.endHallsHook = hooks.CreateHook<ScriptDelegate>(kingdom.ManageRouteLength, endHallsScript->Functions->Function);
                kingdom.endHallsHook.Activate();
                kingdom.endHallsHook.Enable();

                // Modify the kingdoms on the end screen
                var iconsEndScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_victorydefeat_draw_char") - 100000);
                kingdom.fixEndIconsHook = hooks.CreateHook<ScriptDelegate>(kingdom.ModifyEndScreenIcons, iconsEndScript->Functions->Function);
                kingdom.fixEndIconsHook.Activate();
                kingdom.fixEndIconsHook.Enable();
            }
        }

        // TODO: REMOVE Testing function to oneshot
        private void oneShot()
        {
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {
                var endScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_pattern_deal_damage_enemy_subtract") - 100000);
                this.oneShotHook = hooks.CreateHook<ScriptDelegate>(this.killquickly, endScript->Functions->Function);
                this.oneShotHook.Activate();
                this.oneShotHook.Enable();
            }
        }

        // TODO: REMOVE Testing function to oneshot
        internal RValue* killquickly(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            argv[2]->Real = 100000000;
            returnValue = this.oneShotHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Credit to fuzzything44, shamelessly took this from their Fullmoon Arsenal mod
        // Copies a directory to another location
        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        // Copy the Items folder to the RnS mod folder
        private void CopyItemModToRnSMod()
        {
            // Copy over item mod to game folder
            // Assumes that this environment variable is actually correct
            string path = Path.Combine(this.config.Mods, @"RnSArchipelago\ArchipelagoItems");
            CopyDirectory(path, @".\Mods\ArchipelagoItems", true);
        }

        // Changes the outskirts routing to only have shots and chests besides the boss
        private RValue* OutskirtsDetour(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.outskirtsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            var a = new RValue(self);
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
                this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "outskirts", self, returnValue, argc, argv), Color.Red);
                this.logger.PrintMessage(a.ToString(), Color.Red);
            }
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
                    //*argv[0] = new RValue(availablePrimary[random.Next(availablePrimary.Count)]);
                    *argv[0] = new RValue(67);
                } else if (isSecondary(abilityId))
                {
                    //*argv[0] = new RValue(availableSecondary[random.Next(availableSecondary.Count)]);
                    *argv[0] = new RValue(72);
                } else if (isSpecial(abilityId))
                {
                    //*argv[0] = new RValue(availableSpecial[random.Next(availableSpecial.Count)]);
                } else if (isDefensive(abilityId))
                {
                    //*argv[0] = new RValue(availableDefensive[random.Next(availableDefensive.Count)]);
                }

                if (HookUtil.IsEqualToNumeric(argv[2], 1))
                {
                    
                    //*argv[0] = new(324);
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
