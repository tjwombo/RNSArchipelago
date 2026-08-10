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

                        RValue nameValue = new RValue(0);
                        rnsReloaded.CreateString(&nameValue, "ARCHIPELAGO");
                        ModifyElementVariable(element, "nameStr", ModificationType.InsertToArray, nameValue);

                        RValue descValue = new RValue(0);
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
                FindLayer("RunMenu_Options", out var layer);

                if (layer != null)
                {
                    // Find the element in the layer that is the lobby type selector, has name lobby
                    var element = layer->Elements.First;

                    CLayerElementBase* passwordBox = null;
                    while (element != null)
                    {
                        var instance = (CLayerInstanceElement*)element;
                        var instanceValue = new RValue(instance->Instance);

                        switch (rnsReloaded.GetString(instanceValue.Get("text")))
                        {
                            case "LOBBY SETTINGS":
                                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                                {
                                    RValue lobbyVar = new RValue(0);
                                    rnsReloaded.CreateString(&lobbyVar, "ARCHIPELAGO SETTINGS");
                                    ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, lobbyVar);
                                }

                                break;
                            case "name":
                                RValue nameValue = new RValue(0);
                                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                                {
                                    RValue nameVar = new RValue(0);
                                    rnsReloaded.CreateString(&nameVar, "Archipelago name");
                                    ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, nameVar);

                                    rnsReloaded.CreateString(&nameValue, ArchipelagoName);
                                }
                                else
                                {
                                    rnsReloaded.CreateString(&nameValue, originalName);
                                }

                                ModifyElementVariable(element, "defText", ModificationType.ModifyLiteral, nameValue);
                                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = nameValue;

                                break;
                            case "description":
                                RValue descValue = new RValue(0);
                                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                                {
                                    RValue descVar = new RValue(0);
                                    rnsReloaded.CreateString(&descVar, "Archipelago address");
                                    ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, descVar);

                                    rnsReloaded.CreateString(&descValue, ArchipelagoAddress);
                                }
                                else
                                {
                                    rnsReloaded.CreateString(&descValue, originalDesc);
                                }
                                ModifyElementVariable(element, "defText", ModificationType.ModifyLiteral, descValue);
                                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = descValue;

                                break;
                            case "set password:":
                                RValue passValue = new RValue(0);
                                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                                {
                                    RValue passVar = new RValue(0);
                                    rnsReloaded.CreateString(&passVar, "enter password:");
                                    ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, passVar);

                                    rnsReloaded.CreateString(&passValue, ArchipelagoPassword);
                                }
                                else
                                {
                                    rnsReloaded.CreateString(&passValue, originalPass);
                                }
                                *rnsReloaded.utils.GetGlobalVar("lobbyPassword") = passValue;

                                break;
                            case "[ \"no password\",\"password locked\" ]":
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
                                ModifyElementVariable(element, "cursorPos", ModificationType.ModifyLiteral, passVal);
                                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2) = passVal;

                                rnsReloaded.ExecuteScript("scr_runmenu_lobbysettings_passwordlock", instance->Instance, other, 0, argv);

                                break;
                            case "[ \"single player\",\"two players\",\"three players\",\"four players\" ]":
                                if (IsEqualToNumeric(rnsReloaded.utils.GetGlobalVar("obLobbyType"), 3))
                                {
                                    ModifyElementVariable(element, "cursorPos", ModificationType.ModifyLiteral, new RValue(ArchipelagoNum - 1));
                                    rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real = ArchipelagoNum;
                                }
                                else
                                {
                                    ModifyElementVariable(element, "cursorPos", ModificationType.ModifyLiteral, new RValue(originalNum - 1));
                                    rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real = originalNum;
                                }
                                break;
                            default:
                                break;
                        }

                        element = element->Next;
                    }
                    return returnValue;
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
                        // Update the text on the banner
                        FindElementInLayer("name", "click to edit lobby settings", layer, out var lobbyButton);
                        if (lobbyButton == null)
                        {
                            FindElementInLayer("name", "click to edit archipelago settings", layer, out lobbyButton);
                        }
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
                        RValue nameVar = new RValue(0);
                        rnsReloaded.CreateString(&nameVar, ArchipelagoName);
                        ModifyElementVariable(element, "name", ModificationType.ModifyLiteral, nameVar);

                        RValue descVar = new RValue(0);
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
                        RValue nameVar = new RValue(0);
                        rnsReloaded.CreateString(&nameVar, originalName);
                        ModifyElementVariable(element, "name", ModificationType.ModifyLiteral, nameVar);

                        RValue descVar = new RValue(0);
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
    }
}