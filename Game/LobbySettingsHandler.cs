using Reloaded.Hooks.Definitions;
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
        private readonly InventoryHandler inventoryHandler;
        private readonly ArchipelagoConnection conn;

        internal IHook<ScriptDelegate>? archipelagoButtonHook;

        internal IHook<ScriptDelegate>? lobbySettingsDisplayStepHook;

        internal IHook<ScriptDelegate>? archipelagoOptionsHook;

        internal IHook<ScriptDelegate>? setNameHook;
        internal IHook<ScriptDelegate>? setDescHook;
        internal IHook<ScriptDelegate>? setPassHook;
        //internal IHook<ScriptDelegate>? setNumHook;
        internal IHook<ScriptDelegate>? archipelagoOptionsReturnHook;

        internal IHook<ScriptDelegate>? lobbyTitleHook;

        internal IHook<ScriptDelegate>? supressLobbySettingsVisuallyHook;

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

        internal LobbySettingsHandler(
            WeakReference<IRNSReloaded> rnsReloadedRef,
            ILogger logger,
            InventoryHandler inventoryHandler,
            ArchipelagoConnection conn,
            Config.Config modConfig
            )
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
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
            if (archipelagoButtonHook != null)
            {
                returnValue = archipelagoButtonHook.OriginalFunction(self, other, returnValue, argc, argv);
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
                    originalNum = (int)GetNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(4));

                    initialSetup = false;
                }

                FindElementInLayer("RunMenu_Options", out var layer, "name", "LOBBY", out var element);
                if (layer != null)
                {
                    lobbySettingsDisplayStepHook?.Enable();

                    if (element != null)
                    {
                        ModifyElementVariable(element, "nameXSc", ModificationType.ModifyArray, [new RValue(1), new(0.75)]);
                        ModifyElementVariable(element, "nameXSc", ModificationType.InsertToArray, new RValue(0.75));

                        RValue nameValue = new();
                        rnsReloaded.CreateString(&nameValue, "ARCHIPELAGO");
                        ModifyElementVariable(element, "nameStr", ModificationType.InsertToArray, nameValue);

                        RValue descValue = new();
                        rnsReloaded.CreateString(&descValue, "lobby is open for archipelago");
                        ModifyElementVariable(element, "descStr", ModificationType.InsertToArray, descValue);

                        ModifyElementVariable(element, "colorInd", ModificationType.ModifyArray, [new RValue(3), new(8678193)]);

                        ModifyElementVariable(element, "diffXPos", ModificationType.ModifyArray, [new RValue(0), new(-210)]);
                        ModifyElementVariable(element, "diffXPos", ModificationType.ModifyArray, [new RValue(1), new(40)]);
                        ModifyElementVariable(element, "diffXPos", ModificationType.ModifyArray, [new RValue(2), new(290)]);
                        ModifyElementVariable(element, "diffXPos", ModificationType.InsertToArray, new RValue(540));

                        ModifyElementVariable(element, "diffYPos", ModificationType.InsertToArray, new RValue(-20));

                        ModifyElementVariable(element, "maxIndex", ModificationType.ModifyLiteral, new RValue(4));

                        ModifyElementVariable(element, "selectionWidth", ModificationType.ModifyLiteral, new RValue(250));

                        if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                        {
                            ModifyElementVariable(element, "selectIndex", ModificationType.ModifyLiteral, new RValue(3));
                        }
                    }
                }
            }
            return returnValue;
        }

        // Modify the archipelago lobby settings to display appropriate information
        internal RValue* CreateArchipelagoOptions(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (archipelagoOptionsHook != null)
            {
                returnValue = archipelagoOptionsHook.OriginalFunction(self, other, returnValue, argc, argv);
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
                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                    {
                        RValue lobbyVar = new();
                        rnsReloaded.CreateString(&lobbyVar, "ARCHIPELAGO SETTINGS");
                        ModifyElementVariable(lobby, "text", ModificationType.ModifyLiteral, lobbyVar);
                    }
                }

                FindElementInLayer("RunMenu_Options", "text", "name", out var name);
                if (name != null)
                {
                    RValue nameValue = new();
                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                    {
                        RValue nameVar = new();
                        rnsReloaded.CreateString(&nameVar, "Archipelago name");
                        ModifyElementVariable(name, "text", ModificationType.ModifyLiteral, nameVar);

                        rnsReloaded.CreateString(&nameValue, ArchipelagoName);
                    }
                    else
                    {
                        rnsReloaded.CreateString(&nameValue, originalName);
                    }

                    ModifyElementVariable(name, "defText", ModificationType.ModifyLiteral, nameValue);
                    *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = nameValue;
                }

                FindElementInLayer("RunMenu_Options", "text", "description", out var description);
                if (name != null)
                {
                    RValue descValue = new();
                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                    {
                        RValue descVar = new();
                        rnsReloaded.CreateString(&descVar, "Archipelago address");
                        ModifyElementVariable(description, "text", ModificationType.ModifyLiteral, descVar);

                        rnsReloaded.CreateString(&descValue, ArchipelagoAddress);
                    }
                    else
                    {
                        rnsReloaded.CreateString(&descValue, originalDesc);
                    }
                    ModifyElementVariable(description, "defText", ModificationType.ModifyLiteral, descValue);
                    *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = descValue;
                }

                FindElementInLayer("RunMenu_Options", "text", "set password:", out var password);
                if (password != null)
                {
                    RValue passValue = new();
                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                    {
                        RValue passVar = new();
                        rnsReloaded.CreateString(&passVar, "enter password:");
                        ModifyElementVariable(password, "text", ModificationType.ModifyLiteral, passVar);

                        rnsReloaded.CreateString(&passValue, ArchipelagoPassword);
                    }
                    else
                    {
                        rnsReloaded.CreateString(&passValue, originalPass);
                    }
                    *rnsReloaded.utils.GetGlobalVar("lobbyPassword") = passValue;
                }

                FindElementInLayer("RunMenu_Options", "text", "[ \"no password\",\"password locked\" ]", out var passwordValue);
                if (passwordValue != null)
                {
                    RValue passVal = new RValue(0);
                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                    {
                        if (ArchipelagoPassword != "")
                        {
                            passVal = new RValue(1);
                        }
                    }
                    else
                    {
                        if (originalPass != "")
                        {
                            passVal = new RValue(1);
                        }
                    }
                    ModifyElementVariable(passwordValue, "cursorPos", ModificationType.ModifyLiteral, passVal);
                    *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2) = passVal;

                    var instance = (CLayerInstanceElement*)passwordValue;
                    var instanceValue = new RValue(instance->Instance);

                    rnsReloaded.ExecuteScript("scr_runmenu_lobbysettings_passwordlock", instance->Instance, other, 0, argv);
                }

                FindElementInLayer("RunMenu_Options", "text", "[ \"single player\",\"two players\",\"three players\",\"four players\" ]", out var players);
                if (players != null)
                {
                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                    {
                        ModifyElementVariable(players, "cursorPos", ModificationType.ModifyLiteral, new RValue(ArchipelagoNum - 1));
                        rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real = ArchipelagoNum;
                    }
                    else
                    {
                        ModifyElementVariable(players, "cursorPos", ModificationType.ModifyLiteral, new RValue(originalNum - 1));
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
            if (archipelagoOptionsReturnHook != null)
            {
                returnValue = archipelagoOptionsReturnHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                logger.PrintMessage("Unable to call archipleago options return hook", System.Drawing.Color.Red);
            }

            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                {
                    ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                    ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2), 1))
                    {
                        ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));
                    }
                    else
                    {
                        ArchipelagoPassword = "";
                    }

                    ArchipelagoNum = (int)GetNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4));
                }
                else
                {
                    originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                    originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2), 1))
                    {
                        originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));
                    }
                    else
                    {
                        originalPass = "";
                    }

                    originalNum = (int)GetNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4));
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
                RValue nameVal = new(0);
                rnsReloaded.CreateString(&nameVal, originalName);
                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = nameVal;

                RValue descVal = new(0);
                rnsReloaded.CreateString(&descVal, originalDesc);
                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = descVal;

                RValue passVal = new(0);
                rnsReloaded.CreateString(&passVal, originalPass);
                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3) = passVal;

                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4) = new RValue(originalNum);

                rnsReloaded.ExecuteScript("scr_online_save", null, null, []);
            }

            if (lobbyTitleHook != null)
            {
                returnValue = lobbyTitleHook.OriginalFunction(self, other, returnValue, argc, argv);
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
                            if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                            {
                                descriptions = CreateRArray(["the pale keep will be visited if possible.",
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
                                descriptions = CreateRArray(["a route will be taken through the kingdom at random. ",
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
                            ModifyElementVariable(route, "descStr", ModificationType.ModifyLiteral, descriptions);
                        }

                        // Update the text on the banner
                        FindElementInLayer("name", "click to edit lobby settings", layer, out var lobbyButton);
                        if (lobbyButton != null)
                        {
                            if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                            {
                                RValue nameVar = new RValue(0);
                                rnsReloaded.CreateString(&nameVar, "click to edit archipelago settings");
                                ModifyElementVariable(lobbyButton, "name", ModificationType.ModifyLiteral, nameVar);

                            }
                            else if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 1) || IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 2))
                            {
                                RValue lobbyVar = new RValue(0);
                                rnsReloaded.CreateString(&lobbyVar, "click to edit lobby settings");
                                ModifyElementVariable(lobbyButton, "name", ModificationType.ModifyLiteral, lobbyVar);

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
                                var instanceValue = new RValue(instance->Instance);

                                if (UpdateBanner(instanceValue, display))
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
                            var instanceValue = new RValue(instance->Instance);

                            // Update the text on the banner
                            if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                            {
                                archipelagoPassSet = IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2), 1);
                                ArchipelagoNum = (int)GetNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4));
                            }
                            else
                            {
                                originalPassSet = IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2), 1);
                                originalNum = (int)GetNumeric(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4));
                            }

                            // Update the info in the banner
                            if (UpdateBanner(instanceValue, display))
                            {
                                return returnValue;
                            }

                            display = display->Next;
                        }
                    }
                    else
                    {
                        lobbySettingsDisplayStepHook?.OriginalFunction(self, other, returnValue, argc, argv);

                        // Called as a layer step function, so we want to disable it once we leave the screens
                        lobbySettingsDisplayStepHook?.Disable();
                    }

                    return returnValue;
                }
            }

            lobbySettingsDisplayStepHook?.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Update the banner that displays the current lobby settings
        private bool UpdateBanner(RValue instanceValue, CLayerElementBase* element)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (instanceValue.Get("diffTxt") != null &&
                rnsReloaded.GetString(instanceValue.Get("diffTxt")) == "[ \"CUTE\",\"NORMAL\",\"HARD\",\"LUNAR\" ]")
                {
                    if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                    {
                        RValue nameVar = new();
                        rnsReloaded.CreateString(&nameVar, ArchipelagoName);
                        ModifyElementVariable(element, "name", ModificationType.ModifyLiteral, nameVar);

                        RValue descVar = new();
                        rnsReloaded.CreateString(&descVar, ArchipelagoAddress);
                        ModifyElementVariable(element, "descEdit", ModificationType.ModifyLiteral, descVar);

                        ModifyElementVariable(element, "maxPlayers", ModificationType.ModifyLiteral, new RValue(ArchipelagoNum));

                        ModifyElementVariable(element, "passwordLocked", ModificationType.ModifyLiteral, new RValue(archipelagoPassSet && ArchipelagoPassword != ""));

                        // TODO: SET STARTING GOLD BASED OFF OF AMOUNT OF GOLD ITEMS, AND INCREASE CURRENT RUN GOLD WHEN RECIEVING GOLD ITEM
                        // TODO: LOOK INTO A BETTER PLACE FOR THIS TO LIVE
                        // Set starting gold
                        *rnsReloaded.utils.GetGlobalVar("startingGold") = new RValue(10);
                    }
                    else
                    {
                        RValue nameVar = new();
                        rnsReloaded.CreateString(&nameVar, originalName);
                        ModifyElementVariable(element, "name", ModificationType.ModifyLiteral, nameVar);

                        RValue descVar = new();
                        rnsReloaded.CreateString(&descVar, originalDesc);
                        ModifyElementVariable(element, "descEdit", ModificationType.ModifyLiteral, descVar);

                        ModifyElementVariable(element, "maxPlayers", ModificationType.ModifyLiteral, new RValue(originalNum));

                        ModifyElementVariable(element, "passwordLocked", ModificationType.ModifyLiteral, new RValue(originalPassSet && originalPass != ""));

                        // Set starting gold
                        *rnsReloaded.utils.GetGlobalVar("startingGold") = new RValue(10);
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
            setNameHook?.OriginalFunction(self, other, returnValue, argc, argv);
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
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
            setDescHook?.OriginalFunction(self, other, returnValue, argc, argv);
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
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
            setPassHook?.OriginalFunction(self, other, returnValue, argc, argv);
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
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
                if (argv[0]->ToString() == "lobbydisplay" && IsEqualToNumeric(argv[2], -350) && IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                {
                    FindElement("name", "click to edit lobby settings", out var element);
                    var instance = new RValue(((CLayerInstanceElement*)element)->Instance);

                    RValue boxTitle = new RValue(0);
                    if (inventoryHandler.isActive)
                    {
                        rnsReloaded.CreateString(&boxTitle, "connected to archipelago");

                        *instance.Get("spr") = new(28);
                        *instance.Get("spriteOffsetX") = new(-400);
                    }
                    else
                    {
                        rnsReloaded.CreateString(&boxTitle, "disconencted - click to reconnect");

                        *instance.Get("spr") = new(285);
                        *instance.Get("subimg") = new(10);
                        *instance.Get("spriteOffsetX") = new(-525);
                    }
                    *instance.Get("name") = boxTitle;

                    *instance.Get("textScX") = new(1);
                    *instance.Get("textScY") = new(1);

                    *instance.Get("textOffsetY") = new(-10);

                    *instance.Get("height") = new(110);

                    // Attach it to a dummy function so that we can hook it
                    var scoreId = rnsReloaded.CodeFunctionFind("parameter_count");
                    if (scoreId.HasValue)
                    {
                        *instance.Get("funct") = new(scoreId.Value + 100000);
                    }

                    return returnValue;
                }
            }
            supressLobbySettingsVisuallyHook?.OriginalFunction(self, other, returnValue, argc, argv);
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
                var instanceValue = new RValue(instance->Instance);

                var routeIcons = instanceValue.Get("cursorPos");

                return GetNumeric(routeIcons) switch
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