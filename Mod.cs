using System.Diagnostics.CodeAnalysis;
using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

using RnSArchipelago.Config;
using RnSArchipelago.Connection;
using RnSArchipelago.Data;
using RnSArchipelago.Game;
using RnSArchipelago.Utils;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago
{
    public unsafe class Mod : IMod
    {
        private WeakReference<IRNSReloaded>? rnsReloadedRef;
        private WeakReference<IReloadedHooks>? hooksRef;
        private ILogger logger = null!;

        private Configurator configurator = null!;
        private Config.Config config = null!;
        private Random rand = null!;

        private HookHandler hookHandler = null!;
        private InventoryHandler inventoryHandler = null!;
        private ShopItemsHandler shopItemsHandler = null!;
        private RouteHandler routeHandler = null!;
        private MapHandler mapHandler = null!;
        private KingdomHandler kingdomHandler = null!;
        private ClassHandler classHandler = null!;
        private LocationHandler locationHandler = null!;
        private ScoutHandler scoutHandler = null!;
        private ArchipelagoConnection conn = null!;
        private SaveHandler saveHandler = null!;

        private readonly SharedData data = new();

        private string modLocation = "";
        private string modVersion = "";

        // TODO: NOT ACTUALLY USED, JUST USED AS MY TESTING HOOK
        private IHook<ScriptDelegate>? setItemHook;

        private IHook<ScriptDelegate>? archipelagoWebsocketHook;

        private IHook<ScriptDelegate>? selectCharacterAbilitiesHook;
        private readonly List<int> availablePrimary = [];
        private readonly List<int> availableSecondary = [];
        private readonly List<int> availableSpecial = [];
        private readonly List<int> availableDefensive = [];

        internal IHook<ScriptDelegate>? oneShotHook;

        private LobbySettingsHandler? lobby;

        public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig)
        {
            var _loader = (IModLoader)loader;
            this.rnsReloadedRef = _loader.GetController<IRNSReloaded>();
            this.hooksRef = _loader.GetController<IReloadedHooks>();

            this.logger = (ILogger)loader.GetLogger();

            this.configurator = new Configurator(((IModLoader)loader).GetModConfigDirectory(modConfig.ModId));
            this.config = this.configurator.GetConfiguration<Config.Config>(0);
            this.config.ConfigurationUpdated += this.ConfigurationUpdated;

            modLocation = _loader.GetDirectoryForModId("RnSArchipelago");
            modVersion = modConfig.ModVersion;

            if (!this.config.StartUpConfig.SkipItemCreation)
            {
                CopyItemModToRnSMod();
            }

            if (this.IsReady(out var rnsReloaded, out _))
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
                this.IsReady(out var rnsReloaded, out var hooks)
            )
            {
                rand = new Random();

                HookUtil.rnsReloadedRef = rnsReloadedRef;
                HookUtil.logger = logger;

                LocationUtil.rnsReloadedRef = rnsReloadedRef;

                inventoryHandler = new InventoryHandler(logger, data);

                KingdomUtil.rnsReloadedRef = rnsReloadedRef;
                KingdomUtil.inventoryHandler = inventoryHandler;
                KingdomUtil.ResetRandom();

                hookHandler = new HookHandler(rnsReloadedRef, hooks);

                saveHandler = new SaveHandler(rnsReloadedRef, logger, hookHandler);

                scoutHandler = new ScoutHandler(rnsReloadedRef, logger, hookHandler, inventoryHandler);
                conn = new ArchipelagoConnection(rnsReloadedRef, logger, hookHandler, inventoryHandler, saveHandler, scoutHandler, this.config, data);
                scoutHandler.SetConn(conn);
                shopItemsHandler = new ShopItemsHandler(rand, logger, inventoryHandler);
                locationHandler = new LocationHandler(rnsReloadedRef, rand, logger, hookHandler, inventoryHandler, shopItemsHandler, scoutHandler, this.config, conn);
                lobby = new LobbySettingsHandler(rnsReloadedRef, logger, hookHandler, inventoryHandler, conn, this.config);
                routeHandler = new RouteHandler(rnsReloadedRef, logger, hookHandler, inventoryHandler, locationHandler, conn);
                mapHandler = new MapHandler(rnsReloadedRef, logger, hookHandler, inventoryHandler, routeHandler);
                kingdomHandler = new KingdomHandler(rnsReloadedRef, logger, hookHandler, inventoryHandler, routeHandler, this.config);

                classHandler = new ClassHandler(rnsReloadedRef, logger, hookHandler, inventoryHandler);


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
                /*var testId = rnsReloaded.ScriptFindId("scr_gamecontrol_update_adventure");
                var testScript = rnsReloaded.GetScriptData(testId - 100000);
                this.setItemHook = hooks.CreateHook<ScriptDelegate>(this.test, testScript->Functions->Function);
                this.setItemHook.Activate();
                this.setItemHook.Enable();*/

                // TODO: FIND THIS A HOME
                // Start the connection to archipelago
                var menuId = rnsReloaded.ScriptFindId("scr_runmenu_main_startrun_multi");
                var menuScript = rnsReloaded.GetScriptData(menuId - 100000);
                this.archipelagoWebsocketHook = hooks.CreateHook<ScriptDelegate>(this.CreateArchipelagoWebsocket, menuScript->Functions->Function);
                this.archipelagoWebsocketHook.Activate();
                this.archipelagoWebsocketHook.Enable();

                hookHandler.AddArchipelagoOptionsToMenu(lobby, conn); // Adds the options for archipelago

                hookHandler.SetupArchipelagoWebsocket(saveHandler, conn); // Creates the websocket for archipelago
                hookHandler.SetupArchipelagoOnlineMultiplayer(conn); // Create the online multiplayer hooks

                hookHandler.SetupSendBattleAndChestLocations(locationHandler); // Creates the hook to send locations on encounter win and chest open
                hookHandler.SetupArchipelagoItems(locationHandler, scoutHandler); // Creates the archipelago items and puts them in the correct chest

                hookHandler.SetupKingdomManagement(routeHandler, mapHandler, kingdomHandler, locationHandler); // Modifies the route based on current items

                hookHandler.SetupClassSanity(classHandler); // Limits the classes you can play based on current items

                // TODO: REMOVE ONCE DONE TESTING
                //oneShot();

                // TODO: IMPLEMENT RANDOMIZATION OPTION AND GET THE UPGRADES WORKING FOR BASE RANDOM ABILITY
                //RandomizePlayerAbilities(); // randomize the player abilities


                //TODO: GET DATA FROM ARCHIPELAGO
                //TODO: Defender special does not work - needs another defender ability to charge, doesn't handle stored charges correct
                //TODO: Similarly, any defender ability adds a counter to you special that does nothing
                //TODO: Ancient abilities does not work as there is no friend
                var primaryId = -22;
                var secondaryId = -15;
                var specialId = -8;
                var defensiveId = -1;
                while (primaryId < 370)
                {
                    availablePrimary.Add(primaryId += 28);
                    availableSecondary.Add(secondaryId += 28);
                    availableSpecial.Add(specialId += 28);
                    availableDefensive.Add(defensiveId += 28);
                }

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
                this.logger.PrintMessage(HookUtil.PrintHook("network error", self, returnValue, argc, argv), Color.Red);
                if (this.setItemHook != null)
                {
                    returnValue = this.setItemHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call set item hook", System.Drawing.Color.Red);
                }
                this.logger.PrintMessage(new RValue(self).ToString(), Color.Red);
                //this.logger.PrintMessage(HookUtil.FindLayerWithField("name", "connected to archipelago"), Color.Red);
                this.logger.PrintMessage(HookUtil.PrintHook("network error", self, returnValue, argc, argv), Color.Red);
                return returnValue;
            }
            if (this.setItemHook != null)
            {
                returnValue = this.setItemHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call set item hook", System.Drawing.Color.Red);
            }
            return returnValue;
        }

        // Create and validate the websocket connection to archipelago before moving to the character selection room
        private RValue* CreateArchipelagoWebsocket(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (!this.IsReady(out var rnsReloaded, out _)) return returnValue;

            // If the lobby type is archipelago set up the websocket
            if (lobby != null && rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
            {
                this.data.connection.Set<string>("name", lobby.ArchipelagoName);
                this.data.connection.Set<string>("address", lobby.ArchipelagoAddress);
                this.data.connection.Set<string>("numPlayers", $"{lobby.ArchipelagoNum}");
                this.data.connection.Set<string>("password", lobby.ArchipelagoPassword);
                conn.isHost = true;
                _ = conn.StartConnection(true);

                config.StartUpConfig.ArchipelagoName = lobby.ArchipelagoName;
                config.StartUpConfig.ArchipelagoAddress = lobby.ArchipelagoAddress;
                config.StartUpConfig.ArchipelagoPassword = lobby.ArchipelagoPassword;
                config.Save?.Invoke();

                saveHandler.UnlockSaveInfo();

                this.logger.PrintMessage(modVersion, Color.DarkOrange);

                // Setup as if a friends only lobby or solo lobby based on the number of players
                if (lobby.ArchipelagoNum > 1)
                {
                    *rnsReloaded.utils.GetGlobalVar("obLobbyType") = 1;
                }
                else
                {
                    // TODO: Fix this as there was errors stopping a single player lobby
                    //*rnsReloaded.utils.GetGlobalVar("obLobbyType") = 0;
                    *rnsReloaded.utils.GetGlobalVar("obLobbyType") = 1;
                }
                if (this.archipelagoWebsocketHook != null)
                {
                    returnValue = this.archipelagoWebsocketHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call archipelago websocket hook", System.Drawing.Color.Red);
                }

                // Return to archipelago lobby
                *rnsReloaded.utils.GetGlobalVar("obLobbyType") = 3;
            }
            else
            {
                //TODO: RESTORE SAVE FILE ON REACHING LOBBY ROOM / DISBANDING INSTEAD OF ENTERING AP ROOM TYPE
                saveHandler.RestoreSaveInfo();

                // Continue normally
                if (this.archipelagoWebsocketHook != null)
                {
                    returnValue = this.archipelagoWebsocketHook.OriginalFunction(self, other, returnValue, argc, argv);
                }
                else
                {
                    this.logger.PrintMessage("Unable to call archipelago websocket hook", System.Drawing.Color.Red);
                }
            }

            return returnValue;
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
            if (this.oneShotHook != null)
            {
                returnValue = this.oneShotHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call oneshot hook", System.Drawing.Color.Red);
            }
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
            string path = Path.Combine(modLocation, "ArchipelagoItems");
            CopyDirectory(path, @".\Mods\ArchipelagoItems", true);
        }

        // Sets up the hooks to randomize the player abiliteis
        private void RandomizePlayerAbilities()
        {
            if (!this.IsReady(out var rnsReloaded, out var hooks)) return;

            var createItemId = rnsReloaded.ScriptFindId("scr_itemsys_create_item");
            var createItemScript = rnsReloaded.GetScriptData(createItemId - 100000);
            this.selectCharacterAbilitiesHook = hooks.CreateHook<ScriptDelegate>(this.ChooseCharacterAbilities, createItemScript->Functions->Function);
            this.selectCharacterAbilitiesHook.Activate();
            this.selectCharacterAbilitiesHook.Enable();
        }

        // Randomly assign an ability of the same type from the available list of abilities
        private RValue* ChooseCharacterAbilities(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (this.IsReady(out var rnsReloaded, out _))
            {
                int abilityId = (int)rnsReloaded.utils.RValueToLong(argv[0]);
                if (isPrimary(abilityId))
                {
                    //*argv[0] = availablePrimary[random.Next(availablePrimary.Count)];
                    *argv[0] = 67;
                }
                else if (isSecondary(abilityId))
                {
                    //*argv[0] = availableSecondary[random.Next(availableSecondary.Count)];
                    *argv[0] = 72;
                }
                else if (isSpecial(abilityId))
                {
                    //*argv[0] = availableSpecial[random.Next(availableSpecial.Count)];
                }
                else if (isDefensive(abilityId))
                {
                    //*argv[0] = availableDefensive[random.Next(availableDefensive.Count)];
                }

                if (argv[2]->ToInt() == 1)
                {

                    //*argv[0] = 324;
                }
            }

            if (this.selectCharacterAbilitiesHook != null)
            {
                returnValue = this.selectCharacterAbilitiesHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call select character abilities hook", System.Drawing.Color.Red);
            }
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

        private bool isHammermaid(int abilityId)
        {
            return abilityId == 286 || abilityId == 293 || abilityId == 300 || abilityId == 307;
        }

        private bool isPyromancer(int abilityId)
        {
            return abilityId == 314 || abilityId == 321 || abilityId == 328 || abilityId == 335;
        }

        private bool isGrenadier(int abilityId)
        {
            return abilityId == 342 || abilityId == 349 || abilityId == 356 || abilityId == 363;
        }

        private bool isShadow(int abilityId)
        {
            return abilityId == 370 || abilityId == 377 || abilityId == 384 || abilityId == 391;
        }

        // Helper function to check if the weakreference are ready and get the strong reference
        [MemberNotNullWhen(true, nameof(rnsReloadedRef))]
        [MemberNotNullWhen(true, nameof(hooksRef))]
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
                return true;
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