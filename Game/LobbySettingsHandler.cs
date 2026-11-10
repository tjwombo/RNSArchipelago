using Reloaded.Mod.Interfaces;
using RnSArchipelago.Connection;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using static RnSArchipelago.Utils.HookUtil;

namespace RnSArchipelago.Game
{
    internal unsafe class LobbySettingsHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly HookHandler hookHandler;
        private readonly InventoryHandler inventoryHandler;
        private readonly ArchipelagoConnection conn;

        internal string ArchipelagoAddress { get; private set; } = "localhost:38281";
        internal string ArchipelagoName { get; private set; } = "Player1";
        internal string ArchipelagoPassword { get; private set; } = "";
        internal int ArchipelagoNum { get; private set; } = 4;
        private bool archipelagoPassSet = false;

        private string originalDesc = "";
        private string originalName = "";
        private string originalPass = "";
        private int originalNum = 4;
        private bool originalPassSet = false;

        private bool initialSetup = true;

        internal LobbySettingsHandler(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger,
            HookHandler hookHandler, InventoryHandler inventoryHandler, ArchipelagoConnection conn,
            Config.Config modConfig)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.hookHandler = hookHandler;
            this.inventoryHandler = inventoryHandler;
            this.conn = conn;

            ArchipelagoName = modConfig.StartUpConfig.ArchipelagoName;
            ArchipelagoAddress = modConfig.StartUpConfig.ArchipelagoAddress;
            ArchipelagoPassword = modConfig.StartUpConfig.ArchipelagoPassword;
            archipelagoPassSet = ArchipelagoPassword != "";
        }

        // TODO: ENSURE THIS DOESN'T APPEAR IN THE TOYBOX LOBBY
        // Modify the lobby types to have an archipelago option
        internal RValue* CreateArchipelagoLobbyType(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            // Create the object
            if (hookHandler.archipelagoButtonHook != null)
            {
                returnValue = hookHandler.archipelagoButtonHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call archipelago button hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {

                if (initialSetup)
                {
                    // Store the original values
                    originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(0));
                    originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(1));
                    originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
                    originalNum = rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(4)->ToInt();

                    initialSetup = false;
                }

                FindElementInLayer("RunMenu_Options", out var layer, "name", "LOBBY", out var element);
                if (layer != null)
                {
                    hookHandler.lobbySettingsDisplayStepHook?.Enable();

                    if (element != null)
                    {
                        var instance = (CLayerInstanceElement*)element;
                        RValue instanceValue = new(instance->Instance);

                        *instanceValue["nameXSc"]->Get(1) = 0.75;

                        instanceValue["nameXSc"]->ArrayPush(0.75);

                        instanceValue["nameStr"]->ArrayPush("ARCHIPELAGO");

                        instanceValue["descStr"]->ArrayPush("lobby is open for archipelago");

                        *instanceValue["colorInd"]->Get(3) = 8678193;

                        *instanceValue["diffXPos"] = new([-210, 40, 290, 540]);

                        instanceValue["diffYPos"]->ArrayPush(-20);

                        *instanceValue["maxIndex"] = 4;

                        *instanceValue["selectionWidth"] = 250;

                        if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                        {
                            *instanceValue["selectIndex"] = 3;
                        }
                    }
                }
            }
            return returnValue;
        }

        internal RValue* ActivateAllKingdoms(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (hookHandler.kingdomAvailablityHook != null)
            {
                returnValue = hookHandler.kingdomAvailablityHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call activate kingdoms hook", System.Drawing.Color.Red);
            }
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                {
                    *returnValue = 1;
                }
            }
            return returnValue;
        }

        // Modify the archipelago lobby settings to display appropriate information
        internal RValue* CreateArchipelagoOptions(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (hookHandler.archipelagoOptionsHook != null)
            {
                returnValue = hookHandler.archipelagoOptionsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call archipelago options hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                FindElementInLayer("RunMenu_Options", "text", "LOBBY SETTINGS", out var lobby);
                if (lobby != null)
                {
                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                    {
                        var instance = (CLayerInstanceElement*)lobby;
                        RValue instanceValue = new(instance->Instance);
                        *instanceValue["text"] = "ARCHIPELAGO SETTINGS";
                    }
                }

                FindElementInLayer("RunMenu_Options", "text", "name", out var name);
                if (name != null)
                {
                    var instance = (CLayerInstanceElement*)name;
                    RValue instanceValue = new(instance->Instance);

                    RValue nameValue;
                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                    {
                        *instanceValue["text"] = "Archipelago name";

                        nameValue = ArchipelagoName;
                    }
                    else
                    {
                        nameValue = originalName;
                    }

                    *instanceValue["defText"] = nameValue;
                    *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = nameValue;
                }

                FindElementInLayer("RunMenu_Options", "text", "description", out var description);
                if (description != null)
                {
                    var instance = (CLayerInstanceElement*)description;
                    RValue instanceValue = new(instance->Instance);

                    RValue descValue;
                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                    {
                        *instanceValue["text"] = "Archipelago address";

                        descValue = ArchipelagoAddress;
                    }
                    else
                    {
                        descValue = originalDesc;
                    }
                    *instanceValue["defText"] = descValue;
                    *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = descValue;
                }

                FindElementInLayer("RunMenu_Options", "text", "set password:", out var password);
                if (password != null)
                {
                    var instance = (CLayerInstanceElement*)password;
                    RValue instanceValue = new(instance->Instance);

                    RValue passValue;
                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                    {
                        *instanceValue["text"] = "enter password:";

                        passValue = ArchipelagoPassword;
                    }
                    else
                    {
                        passValue = originalPass;
                    }
                    *rnsReloaded.utils.GetGlobalVar("lobbyPassword") = passValue;
                }

                FindElementInLayer("RunMenu_Options", "text", "[ \"no password\",\"password locked\" ]", out var passwordValue);
                if (passwordValue != null)
                {
                    RValue passVal = 0;
                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                    {
                        if (ArchipelagoPassword != "")
                        {
                            passVal = 1;
                        }
                    }
                    else
                    {
                        if (originalPass != "")
                        {
                            passVal = 1;
                        }
                    }
                    var instance = (CLayerInstanceElement*)passwordValue;
                    RValue instanceValue = new(instance->Instance);

                    *instanceValue["cursorPos"] = passVal;
                    *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2) = passVal;

                    rnsReloaded.ExecuteScript("scr_runmenu_lobbysettings_passwordlock", instance->Instance, other, 0, argv);
                }

                FindElementInLayer("RunMenu_Options", "text", "[ \"single player\",\"two players\",\"three players\",\"four players\" ]", out var players);
                if (players != null)
                {
                    var instance = (CLayerInstanceElement*)players;
                    RValue instanceValue = new(instance->Instance);

                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                    {
                        *instanceValue["cursorPos"] = ArchipelagoNum - 1;
                        rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real = ArchipelagoNum;
                    }
                    else
                    {
                        *instanceValue["cursorPos"] = originalNum - 1;
                        rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real = originalNum;
                    }
                }
            }

            return returnValue;
        }

        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        internal RValue* UpdateLobbySettings(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (hookHandler.archipelagoOptionsReturnHook != null)
            {
                returnValue = hookHandler.archipelagoOptionsReturnHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call archipleago options return hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                {
                    ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                    ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                    if (rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->ToInt() == 1)
                    {
                        ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));
                    }
                    else
                    {
                        ArchipelagoPassword = "";
                    }

                    ArchipelagoNum = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->ToInt();
                }
                else
                {
                    originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                    originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                    if (rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->ToInt() == 1)
                    {
                        originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));
                    }
                    else
                    {
                        originalPass = "";
                    }

                    originalNum = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->ToInt();
                }
            }
            return returnValue;
        }

        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        internal RValue* LobbyToTitle(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = originalName;

                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = originalDesc;

                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3) = originalPass;

                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4) = originalNum;

                rnsReloaded.ExecuteScript("scr_online_save", null, null, []);
            }

            if (hookHandler.lobbyTitleHook != null)
            {
                returnValue = hookHandler.lobbyTitleHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call lobby title hook", System.Drawing.Color.Red);
            }
            return returnValue;

        }

        // Change between displaying archieplago settings and regular settings
        internal RValue* UpdateLobbySettingsDisplayStep(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                FindLayer("RunMenu_Options", out var layer);
                if (layer != null)
                {
                    // Banner in the main lobby screen
                    if (layer->Elements.Count == 8)
                    {
                        // Update the kingdom descriptions
                        FindElementInLayer("RunMenu_Options", "name", "PLAN ROUTE", out var route);
                        if (route != null)
                        {
                            RValue descriptions;
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                            {
                                descriptions = new RValue(["the pale keep will be visited if possible.",
                                    "the scholar's nest will be visited first if possible.",
                                    "the king's arsenal will be visited first if possible.",
                                    "the red darkhouse will be visited first if possible.",
                                    "the churchmouse streets will be visited first if possible.",
                                    "the emerald lakeside will be visited first if possible.",
                                    "the darkhouse depths will be visited first if possible.",
                                    "the subterra sanctum will be visited first if possible.",
                                    "atelier aurum will be visited first if possible.",
                                    "the looping hallway will be visited if possible.",
                                    "the kingdom outskirts will be visited first if possible.",
                                    "the crack in the geode will be visited first if possible."]);
                            } else
                            {
                                descriptions = new RValue(["a route will be taken through the kingdom at random. ",
                                    "the scholar's nest will be visited first.",
                                    "the king's arsenal will be visited first.",
                                    "the red darkhouse will be visited first.",
                                    "the churchmouse streets will be visited first.",
                                    "the emerald lakeside will be visited first.",
                                    "the darkhouse depths will be visited first.",
                                    "the subterra sanctum will be visited first.",
                                    "atelier aurum will be visited first.",
                                    "a route will be taken through the extra stages at random. ",
                                    "all stages will be mixed at random.",
                                    "enemies and stages will be mixed at random."]);
                            }

                            var instance = (CLayerInstanceElement*)route;
                            RValue instanceValue = new(instance->Instance);
                            *instanceValue["descStr"] = descriptions;
                        }

                        // Update the text on the banner
                        FindElementInLayer("name", "click to edit lobby settings", layer, out var lobbyButton);
                        if (lobbyButton != null)
                        {
                            var instance = (CLayerInstanceElement*)lobbyButton;
                            RValue instanceValue = new(instance->Instance);

                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                            {
                                RValue nameVar = 0;
                                rnsReloaded.CreateString(&nameVar, "click to edit archipelago settings");
                                *instanceValue["name"] = nameVar;

                            }
                            else if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 1 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 2)
                            {
                                RValue lobbyVar = 0;
                                rnsReloaded.CreateString(&lobbyVar, "click to edit lobby settings");
                                *instanceValue["name"] = lobbyVar;

                            }
                        }

                        // update the info in the banner
                        var room = rnsReloaded.GetCurrentRoom();
                        layer = room->Layers.First;
                        while (layer != null)
                        {
                            var display = layer->Elements.First;
                            while (display != null)
                            {
                                var instance = (CLayerInstanceElement*)display;
                                RValue instanceValue = new(instance->Instance);

                                if (UpdateBanner(instanceValue))
                                {
                                    return returnValue;
                                }

                                display = display->Next;

                            }

                            layer = layer->Next;
                        }

                        return returnValue;
                    }
                    // Banner in the editing lobby screen
                    else if (layer->Elements.Count == 9)
                    {
                        var display = layer->Elements.First;
                        while (display != null)
                        {
                            var instance = (CLayerInstanceElement*)display;
                            RValue instanceValue = new(instance->Instance);

                            // Update the text on the banner
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                            {
                                archipelagoPassSet = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->ToInt() == 1;
                                ArchipelagoNum = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->ToInt();
                            }
                            else
                            {
                                originalPassSet = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->ToInt() == 1;
                                originalNum = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->ToInt();
                            }

                            // Update the info in the banner
                            if (UpdateBanner(instanceValue))
                            {
                                return returnValue;
                            }

                            display = display->Next;
                        }
                    }
                    else
                    {
                        if (hookHandler.lobbySettingsDisplayStepHook != null)
                        {
                            returnValue = hookHandler.lobbySettingsDisplayStepHook.OriginalFunction(self, other, returnValue, argc, argv);
                        }
                        else
                        {
                            logger.PrintMessage("Unable to call lobby settings display hook", System.Drawing.Color.Red);
                        }

                        // Called as a layer step function, so we want to disable it once we leave the screens
                        hookHandler.lobbySettingsDisplayStepHook?.Disable();
                    }

                    return returnValue;
                }
            }

            if (hookHandler.lobbySettingsDisplayStepHook != null)
            {
                returnValue = hookHandler.lobbySettingsDisplayStepHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call lobby settings display hook", System.Drawing.Color.Red);
            }
            return returnValue;
        }

        // Update the banner that displays the current lobby settings
        private bool UpdateBanner(RValue instanceValue)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (instanceValue["diffTxt"] != null &&
                rnsReloaded.GetString(instanceValue["diffTxt"]) == "[ \"CUTE\",\"NORMAL\",\"HARD\",\"LUNAR\" ]")
                {
                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                    {
                        *instanceValue["name"] = ArchipelagoName;

                        *instanceValue["descEdit"] = ArchipelagoAddress;

                        *instanceValue["maxPlayers"] = ArchipelagoNum;

                        *instanceValue["passwordLocked"] = (archipelagoPassSet && ArchipelagoPassword != "");

                        // TODO: SET STARTING GOLD BASED OFF OF AMOUNT OF GOLD ITEMS, AND INCREASE CURRENT RUN GOLD WHEN RECIEVING GOLD ITEM
                        // TODO: LOOK INTO A BETTER PLACE FOR THIS TO LIVE
                        // Set starting gold
                        *rnsReloaded.utils.GetGlobalVar("startingGold") = 10;
                    }
                    else
                    {
                        *instanceValue["name"] = originalName;

                        *instanceValue["descEdit"] = originalDesc;

                        *instanceValue["maxPlayers"] = originalNum;

                        *instanceValue["passwordLocked"] = (originalPassSet && originalPass != "");

                        // Set starting gold
                        *rnsReloaded.utils.GetGlobalVar("startingGold") = 10;
                    }
                    return true;
                }
            }
            return false;
        }

        // Update the lobby settings name
        internal RValue* UpdateLobbySettingsName(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (hookHandler.setNameHook != null)
            {
                returnValue = hookHandler.setNameHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call set name hook", System.Drawing.Color.Red);
            }
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                {

                    ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));
                }
                else
                {
                    originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));
                }
            }

            return returnValue;
        }

        // Update the lobby settings description
        internal RValue* UpdateLobbySettingsDesc(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (hookHandler.setDescHook != null)
            {
                returnValue = hookHandler.setDescHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call set desc hook", System.Drawing.Color.Red);
            }
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                {

                    ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));
                }
                else
                {
                    originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));
                }
            }

            return returnValue;
        }

        // Update the lobby settings password
        internal RValue* UpdateLobbySettingsPass(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (hookHandler.setPassHook != null)
            {
                returnValue = hookHandler.setPassHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call set pass hook", System.Drawing.Color.Red);
            }
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                {

                    ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
                }
                else
                {
                    originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
                }
            }

            return returnValue;
        }

        internal RValue* SupressLobbySettingsVisually(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (argv[0]->ToString() == "lobbydisplay" && argv[2]->ToInt() == -350 && rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                {
                    var scoreId = rnsReloaded.CodeFunctionFind("parameter_count");

                    if (!scoreId.HasValue)
                    {
                        this.logger.PrintMessage("Could not assign reconnection hook to button", System.Drawing.Color.Red);
                        return returnValue;
                    }

                    CLayerElementBase* element;

                    if (conn.isHost)
                    {
                        FindElement("name", "click to edit lobby settings", out element);
                    } else
                    {
                        FindElement("funct", (scoreId.Value + 100000) + "", out element);

                        // Destroy the existing (if any) reconnect button so we can create a new one, as this function errors if we dont create the button or call the original function as a client
                        if (element != null)
                        {
                            // TODO: ACTUALLY DESTROY THE INSTANCE RATHER THAN CHANGE THE FUNCTION SO WE DONT FIND IT IN THE SEARCH
                            RValue destroyInstance = ((CLayerInstanceElement*)element)->Instance;
                            *destroyInstance["funct"] = -1;
                        }

                        var spr = rnsReloaded.ExecuteCodeFunction("asset_get_index", null, null, ["spr_empty"]);
                        rnsReloaded.ExecuteScript("scr_runmenu_make_button", self, other, ["lobbysettings", "", spr!.Value, 0, 0, -360, 1400, 110, scoreId.Value + 100000]);
                        FindElement("funct", (scoreId.Value + 100000) + "", out element);
                    }

                    RValue instance = ((CLayerInstanceElement*)element)->Instance;

                    if (inventoryHandler.isActive)
                    {
                        *instance["name"] = "connected to archipelago";

                        *instance["spr"] = 28;
                        *instance["spriteOffsetX"] = -400;
                    }
                    else
                    {
                        *instance["name"] = "disconencted - click to reconnect";

                        *instance["spr"] = 285;
                        *instance["subimg"] = 10;
                        *instance["spriteOffsetX"] = -525;
                    }

                    *instance["textScX"] = 1;
                    *instance["textScY"] = 1;

                    *instance["textOffsetY"] = -10;

                    *instance["mouseob_height"] = 110;
                    *instance["height"] = 110;

                    // Attach it to a dummy function so that we can hook it
                    *instance["funct"] = scoreId.Value + 100000;

                    return returnValue;
                }
            }
            if (hookHandler.supressLobbySettingsVisuallyHook != null)
            {
                returnValue = hookHandler.supressLobbySettingsVisuallyHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call supress lobby settings visual hook", System.Drawing.Color.Red);
            }
            return returnValue;
        }

        internal static string GetSelectedKingdom()
        {
            FindElementInLayer("RunMenu_Options", "name", "PLAN ROUTE", out var lobby);

            if (lobby == null)
            {
                return "";
            }
            else
            {
                var instance = (CLayerInstanceElement*)lobby;
                RValue instanceValue = new(instance->Instance);

                return instanceValue["cursorPos"]->ToLong() switch
                {
                    0 => "hw_keep",
                    1 => "hw_nest",
                    2 => "hw_arsenal",
                    3 => "hw_lighthouse",
                    4 => "hw_streets",
                    5 => "hw_lakeside",
                    6 => "hw_depths",
                    7 => "hw_sanct",
                    8 => "hw_aurum",
                    9 => "hw_darkhall",
                    10 => "hw_outskirts",
                    11 => "hw_geode",
                    _ => "",
                };
            }
        }
    }
}