using Reloaded.Hooks.Definitions;
using RnSArchipelago.Connection;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class HookHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly IReloadedHooks hooks;

        // Archipelago Connection
        internal IHook<ScriptDelegate>? resetConnHook;
        internal IHook<ScriptDelegate>? reconnectHook;
        internal IHook<ScriptDelegate>? parseSteamPacketHook;
        internal IHook<ScriptDelegate>? sendClientConnectionHook;
        internal IHook<ScriptDelegate>? sendClientHallwayHook;
        internal IHook<ScriptDelegate>? handleClientDisconnectHook;

        // Message Handler
        internal IHook<ScriptDelegate>? addMessageHook;

        // Class Handler
        internal IHook<ScriptDelegate>? lockClassHook;
        internal IHook<ScriptDelegate>? mouseClassHook;
        internal IHook<ScriptDelegate>? stopColorHook;

        // Route Handler
        internal IHook<ScriptDelegate>? chooseHallsHook;
        internal IHook<ScriptDelegate>? endHallsHook;
        internal IHook<ScriptDelegate>? stopXSubimgHook;

        // Kingdom Handler
        internal IHook<ScriptDelegate>? fixEndIconsHook;
        internal IHook<ScriptDelegate>? changeStartingKingdomBackgroundScriptHook;

        // Map Handler
        internal IHook<ScriptDelegate>? fixChooseIconsHook;

        // Lobby Settings Handler
        internal IHook<ScriptDelegate>? archipelagoButtonHook;
        internal IHook<ScriptDelegate>? lobbySettingsDisplayStepHook;
        internal IHook<ScriptDelegate>? archipelagoOptionsHook;
        internal IHook<ScriptDelegate>? kingdomAvailablityHook;
        internal IHook<ScriptDelegate>? setNameHook;
        internal IHook<ScriptDelegate>? setDescHook;
        internal IHook<ScriptDelegate>? setPassHook;
        //internal IHook<ScriptDelegate>? setNumHook;
        internal IHook<ScriptDelegate>? archipelagoOptionsReturnHook;
        internal IHook<ScriptDelegate>? lobbyTitleHook;
        internal IHook<ScriptDelegate>? supressLobbySettingsVisuallyHook;

        // Save Handler
        internal IHook<ScriptDelegate>? originalUnlockHook;

        // Location Handler
        internal IHook<ScriptDelegate>? notchCompleteHook;
        internal IHook<ScriptDelegate>? chestOpenHook;
        internal IHook<ScriptDelegate>? setupItemsHook;
        internal IHook<ScriptDelegate>? enableModHook;
        internal IHook<ScriptDelegate>? itemAmtHook;
        internal IHook<ScriptDelegate>? itemGetHook;
        internal IHook<ScriptDelegate>? itemSetHook;
        internal IHook<ScriptDelegate>? itemSetDescriptionHook;
        //internal IHook<ScriptDelegate>? itemSetUpgradeDescriptionHook;
        internal IHook<ScriptDelegate>? takeItemHook;
        internal IHook<ScriptDelegate>? RemoveAPItemTieHook;
        internal IHook<ScriptDelegate>? spawnTreasuresphereOnStartHook;
        internal IHook<ScriptDelegate>? readyCheckHook;
        internal IHook<ScriptDelegate>? openShopHook;
        internal IHook<ScriptDelegate>? closeShopHook;
        internal IHook<ScriptDelegate>? restockShopHook;

        // Scout Handler
        internal IHook<ScriptDelegate>? itemScoutShopHook;

        public HookHandler(WeakReference<IRNSReloaded> rnsReloadedRef, IReloadedHooks hooks) {
            this.rnsReloadedRef = rnsReloadedRef;
            this.hooks = hooks;
        }

        // Set up the hooks relating to archipelago options
        internal void AddArchipelagoOptionsToMenu(LobbySettingsHandler lobby, ArchipelagoConnection conn)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded) || lobby == null) return;

            // Create the archipelago lobby type button
            var menuId = rnsReloaded.ScriptFindId("scr_runmenu_make_lobby_select");
            var menuScript = rnsReloaded.GetScriptData(menuId - 100000);
            this.archipelagoButtonHook = hooks.CreateHook<ScriptDelegate>(lobby.CreateArchipelagoLobbyType, menuScript->Functions->Function);
            this.archipelagoButtonHook.Activate();
            this.archipelagoButtonHook.Enable();

            // Create the archipelago banner
            var optionsId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_setup");
            var optionsScript = rnsReloaded.GetScriptData(optionsId - 100000);
            this.archipelagoOptionsHook = hooks.CreateHook<ScriptDelegate>(lobby.CreateArchipelagoOptions, optionsScript->Functions->Function);
            this.archipelagoOptionsHook.Activate();
            this.archipelagoOptionsHook.Enable();

            // Make all kingdoms available while under the AP lobby type
            var availableId = rnsReloaded.ScriptFindId("scr_stagefirst_available");
            var availableScript = rnsReloaded.GetScriptData(availableId - 100000);
            this.kingdomAvailablityHook = hooks.CreateHook<ScriptDelegate>(lobby.ActivateAllKingdoms, availableScript->Functions->Function);
            this.kingdomAvailablityHook.Activate();
            this.kingdomAvailablityHook.Enable();

            // Update the info banner through a step function
            var displayId = rnsReloaded.ScriptFindId("gameframe_draw");
            var displayScript = rnsReloaded.GetScriptData(displayId - 100000);
            this.lobbySettingsDisplayStepHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsDisplayStep, displayScript->Functions->Function);
            this.lobbySettingsDisplayStepHook.Activate();


            // Update the name if entered before returning
            var nameId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_name");
            var nameScript = rnsReloaded.GetScriptData(nameId - 100000);
            this.setNameHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsName, nameScript->Functions->Function);
            this.setNameHook.Activate();
            this.setNameHook.Enable();

            // Update the description if entered before returning
            var descId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_desc");
            var descScript = rnsReloaded.GetScriptData(descId - 100000);
            this.setDescHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsDesc, descScript->Functions->Function);
            this.setDescHook.Activate();
            this.setDescHook.Enable();

            // Update the password if entered before returning
            var passId = rnsReloaded.ScriptFindId("textboxcomp_set_password");
            var passScript = rnsReloaded.GetScriptData(passId - 100000);
            this.setPassHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettingsPass, passScript->Functions->Function);
            this.setPassHook.Activate();
            this.setPassHook.Enable();

            // Update all the settings after returning
            var returnId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_return");
            var returnScript = rnsReloaded.GetScriptData(returnId - 100000);
            this.archipelagoOptionsReturnHook = hooks.CreateHook<ScriptDelegate>(lobby.UpdateLobbySettings, returnScript->Functions->Function);
            this.archipelagoOptionsReturnHook.Activate();
            this.archipelagoOptionsReturnHook.Enable();

            // TODO: PROBABLY DOESN'T FIRE IF GAME CRASHES AND THAT CAUSES ARCHIPELAGO SETTINGS TO PERSIST IN REGULAR
            // Restore settings to original when returned to the main menu
            var titleId = rnsReloaded.ScriptFindId("scr_runmenu_char_return");
            var titleScript = rnsReloaded.GetScriptData(titleId - 100000);
            this.lobbyTitleHook = hooks.CreateHook<ScriptDelegate>(lobby.LobbyToTitle, titleScript->Functions->Function);
            this.lobbyTitleHook.Activate();
            this.lobbyTitleHook.Enable();

            // Disables the lobby settings info while mid run
            var visualSettingsId = rnsReloaded.ScriptFindId("scr_runmenu_make_lobbysettings");
            var visualSettingsScript = rnsReloaded.GetScriptData(visualSettingsId - 100000);
            this.supressLobbySettingsVisuallyHook = hooks.CreateHook<ScriptDelegate>(lobby.SupressLobbySettingsVisually, visualSettingsScript->Functions->Function);
            this.supressLobbySettingsVisuallyHook.Activate();
            this.supressLobbySettingsVisuallyHook.Enable();

            // Hook into an unused function to insert our own reconnect function
            var paramId = rnsReloaded.CodeFunctionFind("parameter_count");
            if (paramId.HasValue)
            {
                var paramScript = rnsReloaded.GetScriptData(paramId.Value);
                this.reconnectHook = hooks.CreateHook<ScriptDelegate>(conn.Reconnect, paramScript->Functions->Function);
                this.reconnectHook.Activate();
                this.reconnectHook.Enable();
            }
        }

        // Set up the hooks to set up the websocket
        internal void SetupArchipelagoWebsocket(SaveHandler saveHandler, ArchipelagoConnection conn)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Store the initial unlock keys configuration
            var saveId = rnsReloaded.ScriptFindId("scr_unlock_read_save");
            var saveScript = rnsReloaded.GetScriptData(saveId - 100000);
            this.originalUnlockHook = hooks.CreateHook<ScriptDelegate>(saveHandler.RecordSaveInfo, saveScript->Functions->Function);
            this.originalUnlockHook.Activate();
            this.originalUnlockHook.Enable();

            // Close the AP connection
            var resetAPConnId = rnsReloaded.ScriptFindId("scr_socketmap_clear_and_disconnect");
            var resetAPConnScript = rnsReloaded.GetScriptData(resetAPConnId - 100000);
            this.resetConnHook = hooks.CreateHook<ScriptDelegate>(conn.ResetConn, resetAPConnScript->Functions->Function);
            this.resetConnHook.Activate();
            this.resetConnHook.Enable();

            // Set up in game message queue checking
            var drawId = rnsReloaded.ScriptFindId("scr_gamecontrol_update_adventure");
            var drawScript = rnsReloaded.GetScriptData(drawId - 100000);
            this.addMessageHook = hooks.CreateHook<ScriptDelegate>(conn.messageHandler.AddMessage, drawScript->Functions->Function);
            this.addMessageHook.Activate();
            this.addMessageHook.Enable();
        }

        // Set up the hooks related to multiplayer clients
        internal void SetupArchipelagoOnlineMultiplayer(ArchipelagoConnection conn)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Start the connection to archipelago
            var connectId = rnsReloaded.ScriptFindId("scr_lobbyhost_buff_client");
            var connectScript = rnsReloaded.GetScriptData(connectId - 100000);
            this.sendClientConnectionHook = hooks.CreateHook<ScriptDelegate>(conn.SendClientConnection, connectScript->Functions->Function);
            this.sendClientConnectionHook.Activate();
            this.sendClientConnectionHook.Enable();

            // Handle host communication
            var packetId = rnsReloaded.ScriptFindId("scr_steam_handle_packet");
            var packetScript = rnsReloaded.GetScriptData(packetId - 100000);
            this.parseSteamPacketHook = hooks.CreateHook<ScriptDelegate>(conn.ParseSteamPacket, packetScript->Functions->Function);
            this.parseSteamPacketHook.Activate();
            this.parseSteamPacketHook.Enable();

            // Handle client disconnect
            var disconnectId = rnsReloaded.ScriptFindId("scr_clientmap_delete");
            var disconnectScript = rnsReloaded.GetScriptData(disconnectId - 100000);
            this.handleClientDisconnectHook = hooks.CreateHook<ScriptDelegate>(conn.HandleClientDisconnect, disconnectScript->Functions->Function);
            this.handleClientDisconnectHook.Activate();
            this.handleClientDisconnectHook.Enable();

        }

        // Set up the hooks to send locations through the websocket
        internal void SetupSendBattleAndChestLocations(LocationHandler locationHandler)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Send out locations on encounter win
            var battleWonScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_notchexbattle_victory_transfer") - 100000);
            this.notchCompleteHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SendNotchComplete, battleWonScript->Functions->Function);
            this.notchCompleteHook.Activate();
            this.notchCompleteHook.Enable();

            // Send out locations on chest open
            var chestOpenScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_npc_treasure_explode") - 100000);
            this.chestOpenHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SendChestOpen, chestOpenScript->Functions->Function);
            this.chestOpenHook.Activate();
            this.chestOpenHook.Enable();

            // Remove AP items on ties
            var tieScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemlootroll_update_roll") - 100000);
            this.RemoveAPItemTieHook = hooks.CreateHook<ScriptDelegate>(locationHandler.RemoveAPItemTie, tieScript->Functions->Function);
            this.RemoveAPItemTieHook.Activate();
            this.RemoveAPItemTieHook.Enable();
        }

        // Set up the hooks to manipulate chest and shop items
        internal void SetupArchipelagoItems(LocationHandler locationHandler, ScoutHandler scoutHandler)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Setup the mod items that corresponds to the items in CopyItemModToRnSMod()
            var setupItemsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_init_mods") - 100000);
            this.setupItemsHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetupArchipelagoItems, setupItemsScript->Functions->Function);
            this.setupItemsHook.Activate();
            this.setupItemsHook.Enable();

            // Make it so that the archipelago items mod cannot be disabled
            var modApplyScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_runmenu_mods_apply") - 100000);
            this.enableModHook = hooks.CreateHook<ScriptDelegate>(locationHandler.EnableArchipelagoItems, modApplyScript->Functions->Function);
            this.enableModHook.Activate();
            this.enableModHook.Enable();

            // Set amount of items in chest, 5 for archipelago, normal for chest # for normal
            var itemAmtScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_loot_amount") - 100000);
            this.itemAmtHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetAmountOfItems, itemAmtScript->Functions->Function);
            this.itemAmtHook.Activate();
            this.itemAmtHook.Enable();

            // Get the id of the archipelago item 
            var itemGetScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_readsheet_items") - 100000);
            this.itemGetHook = hooks.CreateHook<ScriptDelegate>(locationHandler.GetItems, itemGetScript->Functions->Function);
            this.itemGetHook.Activate();
            this.itemGetHook.Enable();

            // Scout the archipelago item to display values in a shop
            var itemScoutShopScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_populate_store") - 100000);
            this.itemScoutShopHook = hooks.CreateHook<ScriptDelegate>(scoutHandler.ScoutShopItems, itemScoutShopScript->Functions->Function);
            this.itemScoutShopHook.Activate();
            this.itemScoutShopHook.Enable();

            // Set the item to be an archipelago item
            var itemSetScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_create_item") - 100000);
            this.itemSetHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetItems, itemSetScript->Functions->Function);
            this.itemSetHook.Activate();
            this.itemSetHook.Enable();

            // Set the description of an item to match its archipelago item
            var itemSetDescriptionScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_infodraw_get_item_desc") - 100000);
            this.itemSetDescriptionHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetItemsDescription, itemSetDescriptionScript->Functions->Function);
            this.itemSetDescriptionHook.Activate();
            this.itemSetDescriptionHook.Enable();

            //TODO: FINISH GETTING IT WORKING
            // Set the shop upgrade description to match the class that ability is from
            /*var itemSetUpgradeDescriptionId = rnsReloaded.ScriptFindId("scr_infodraw_get_item_desc");
                var itemSetUpgradeDescriptionScript = rnsReloaded.GetScriptData(itemSetUpgradeDescriptionId - 100000);
                locationHandler.itemSetUpgradeDescriptionHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SetUpgradeDescription, itemSetUpgradeDescriptionScript->Functions->Function);
                locationHandler.itemSetUpgradeDescriptionHook.Activate();
                locationHandler.itemSetUpgradeDescriptionHook.Enable();*/

            // Prevents you from actually taking an item, and sends out the corresponding location
            var takeItemScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_pickup_loot") - 100000);
            this.takeItemHook = hooks.CreateHook<ScriptDelegate>(locationHandler.TakeItem, takeItemScript->Functions->Function);
            this.takeItemHook.Activate();
            this.takeItemHook.Enable();

            // Give treasurespheres that have accumulated 
            var treasuresphereOnStartScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_generate") - 100000);
            this.spawnTreasuresphereOnStartHook = hooks.CreateHook<ScriptDelegate>(locationHandler.SpawnTreasuresphereOnStart, treasuresphereOnStartScript->Functions->Function);
            this.spawnTreasuresphereOnStartHook.Activate();
            this.spawnTreasuresphereOnStartHook.Enable();

            // Activates the restock shop hook when the store is opened
            var openShopScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_open_store") - 100000);
            this.openShopHook = hooks.CreateHook<ScriptDelegate>(locationHandler.OpenShop, openShopScript->Functions->Function);
            this.openShopHook.Activate();
            this.openShopHook.Enable();

            // Deactivates the restock shop hook when the store is closed
            var closeShopScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_close_store") - 100000);
            this.closeShopHook = hooks.CreateHook<ScriptDelegate>(locationHandler.CloseShop, closeShopScript->Functions->Function);
            this.closeShopHook.Activate();
            this.closeShopHook.Enable();

            // Replacese the newly bought AP item in the shop
            var restockShopScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_itemsys_draw_itemrow") - 100000);
            this.restockShopHook = hooks.CreateHook<ScriptDelegate>(locationHandler.RestockShop, restockShopScript->Functions->Function);
            this.restockShopHook.Activate();

        }

        // Set up the hooks for class sanity handling
        internal void SetupClassSanity(ClassHandler classHandler)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Visually lock characters not yet obtained
            var lockVisualClassScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_charselect2_draw_menu") - 100000);
            this.lockClassHook = hooks.CreateHook<ScriptDelegate>(classHandler.SetClassAvailability, lockVisualClassScript->Functions->Function);
            this.lockClassHook.Activate();
            this.lockClassHook.Enable();

            // Lock characters not yet obtained on mouse click
            var lockClassScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_charselect2_update_choosecolor") - 100000);
            this.mouseClassHook = hooks.CreateHook<ScriptDelegate>(classHandler.LockClass, lockClassScript->Functions->Function);
            this.mouseClassHook.Activate();
            this.mouseClassHook.Enable();

            // Prevent the drawing of colors if trying to select a locked class when doing a mouse click on a locked class
            var stopColorScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_charselect2_setup_colors") - 100000);
            this.stopColorHook = hooks.CreateHook<ScriptDelegate>(classHandler.StopColorDraw, stopColorScript->Functions->Function);
            this.stopColorHook.Activate();
            this.stopColorHook.Enable();
        }

        // Set up the hooks for kingdom sanity handling
        internal void SetupKingdomManagement(RouteHandler routeHandler, MapHandler mapHandler, KingdomHandler kingdomHandler, LocationHandler locationHandler)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Create the planned route
            var chooseHallsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_choose_halls") - 100000);
            this.chooseHallsHook = hooks.CreateHook<ScriptDelegate>(routeHandler.CreateRoute, chooseHallsScript->Functions->Function);
            this.chooseHallsHook.Activate();
            this.chooseHallsHook.Enable();

            // Modify the icons on the route selection screen
            var iconsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_stagefirst_get_subimg") - 100000);
            this.fixChooseIconsHook = hooks.CreateHook<ScriptDelegate>(mapHandler.ModifyRouteIcons, iconsScript->Functions->Function);
            this.fixChooseIconsHook.Activate();
            this.fixChooseIconsHook.Enable();

            // Make sure you can go to the next hallway and perform additional functions that need to run every room
            var endHallsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_move_next") - 100000);
            this.endHallsHook = hooks.CreateHook<ScriptDelegate>(routeHandler.MoveNext, endHallsScript->Functions->Function);
            this.endHallsHook.Activate();
            this.endHallsHook.Enable();

            // Modify the kingdoms on the end screen
            var iconsEndScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_victorydefeat_draw_char") - 100000);
            this.fixEndIconsHook = hooks.CreateHook<ScriptDelegate>(kingdomHandler.ModifyEndScreenIcons, iconsEndScript->Functions->Function);
            this.fixEndIconsHook.Activate();
            this.fixEndIconsHook.Enable();

            // Modifies the notch xsubimg
            var xSubimgScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_subimgs") - 100000);
            this.stopXSubimgHook = hooks.CreateHook<ScriptDelegate>(routeHandler.StopXSubimg, xSubimgScript->Functions->Function);
            this.stopXSubimgHook.Activate();
            this.stopXSubimgHook.Enable();

            // WIP FUNCTION
            /*// Modifys 
            var iconsEndScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_change_stage") - 100000);
            kingdom.fixEndIconsHook = hooks.CreateHook<ScriptDelegate>(kingdom.ModifyEndScreenIcons, iconsEndScript->Functions->Function);
            kingdom.fixEndIconsHook.Activate();
            kingdom.fixEndIconsHook.Enable();*/

            // Changes the background of the starting kingdom on run start
            var changeStartingKingdomBackgroundScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_charselect2_start_run") - 100000);
            this.changeStartingKingdomBackgroundScriptHook = hooks.CreateHook<ScriptDelegate>(kingdomHandler.ChangeStartingKingdom, changeStartingKingdomBackgroundScript->Functions->Function);
            this.changeStartingKingdomBackgroundScriptHook.Activate();
            this.changeStartingKingdomBackgroundScriptHook.Enable();

            // Removes option to go to next stage if disconnected
            var readyCheckScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_should_update") - 100000);
            this.readyCheckHook = hooks.CreateHook<ScriptDelegate>(locationHandler.StopReadyCheck, readyCheckScript->Functions->Function);
            this.readyCheckHook.Activate();
            this.readyCheckHook.Enable();
        }
    }
}
