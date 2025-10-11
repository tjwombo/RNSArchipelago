using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using static RnSArchipelago.Utils.HookUtil;
using RnSArchipelago.Data;

namespace RnSArchipelago
{
    internal unsafe class LobbySettings
    {
        private readonly IRNSReloaded rnsReloaded;
        private readonly ILoggerV1 logger;
        private readonly IReloadedHooks hooks;
        private readonly SharedData data;

        internal IHook<ScriptDelegate>? archipelagoButtonHook;

        internal IHook<ScriptDelegate>? lobbySettingsDisplayStepHook;

        internal IHook<ScriptDelegate>? archipelagoOptionsHook;

        internal IHook<ScriptDelegate>? setNameHook;
        internal IHook<ScriptDelegate>? setDescHook;
        internal IHook<ScriptDelegate>? setPassHook;
        //internal IHook<ScriptDelegate>? setNumHook;
        internal IHook<ScriptDelegate>? archipelagoOptionsReturnHook;

        internal IHook<ScriptDelegate>? lobbyTitleHook;

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

        internal LobbySettings(IRNSReloaded rnsReloaded, ILoggerV1 logger, IReloadedHooks hooks, SharedData data)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
            this.hooks = hooks;
            this.data = data;
        }

        // TODO: ENSURE THIS DOESN'T APPEAR IN THE TOYBOX LOBBY
        // Modify the lobby types to have an archipelago option
        internal RValue* CreateArchipelagoLobbyType(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            // Create the object
            returnValue = this.archipelagoButtonHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (initialSetup)
            {
                // Store the original values
                originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(0));
                originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(1));
                originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
                originalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(4)->Real;

                initialSetup = false;
            }

            FindElementInLayer(rnsReloaded, "RunMenu_Options", out var layer, "LOBBY", out var element);
            if (layer != null)
            {
                // Attach a layer script to hook into to act as a step hook
                var stepId = rnsReloaded.CodeFunctionFind("os_get_info");
                if (stepId.HasValue)
                {
                    layer->BeginScript.Real = stepId.Value;
                    this.lobbySettingsDisplayStepHook!.Enable();
                }

                if (element != null)
                {
                    ModifyElementVariable(rnsReloaded, element, "nameXSc", ModificationType.ModifyArray, [new RValue(1), new(0.75)]);
                    ModifyElementVariable(rnsReloaded, element, "nameXSc", ModificationType.InsertToArray, new RValue(0.75));

                    RValue nameValue = new RValue(0);
                    rnsReloaded.CreateString(&nameValue, "ARCHIPELAGO");
                    ModifyElementVariable(rnsReloaded, element, "nameStr", ModificationType.InsertToArray, nameValue);

                    RValue descValue = new RValue(0);
                    rnsReloaded.CreateString(&descValue, "lobby is open for archipelago");
                    ModifyElementVariable(rnsReloaded, element, "descStr", ModificationType.InsertToArray, descValue);

                    ModifyElementVariable(rnsReloaded, element, "colorInd", ModificationType.ModifyArray, [new RValue(3), new(8678193)]);

                    ModifyElementVariable(rnsReloaded, element, "diffXPos", ModificationType.ModifyArray, [new RValue(0), new(-210)]);
                    ModifyElementVariable(rnsReloaded, element, "diffXPos", ModificationType.ModifyArray, [new RValue(1), new(40)]);
                    ModifyElementVariable(rnsReloaded, element, "diffXPos", ModificationType.ModifyArray, [new RValue(2), new(290)]);
                    ModifyElementVariable(rnsReloaded, element, "diffXPos", ModificationType.InsertToArray, new RValue(540));

                    ModifyElementVariable(rnsReloaded, element, "diffYPos", ModificationType.InsertToArray, new RValue(-20));

                    ModifyElementVariable(rnsReloaded, element, "maxIndex", ModificationType.ModifyLiteral, new RValue(4));

                    ModifyElementVariable(rnsReloaded, element, "selectionWidth", ModificationType.ModifyLiteral, new RValue(250));

                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                    {
                        ModifyElementVariable(rnsReloaded, element, "selectIndex", ModificationType.ModifyLiteral, new RValue(3));
                    }

                    return returnValue;
                }
            }
            return returnValue;
        }

        // Modify the archipelago lobby settings to display appropriate information
        internal RValue* CreateArchipelagoOptions(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.archipelagoOptionsHook!.OriginalFunction(self, other, returnValue, argc, argv);

            FindLayer(rnsReloaded, "RunMenu_Options", out var layer);

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
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                            {
                                RValue lobbyVar = new RValue(0);
                                rnsReloaded.CreateString(&lobbyVar, "ARCHIPELAGO SETTINGS");
                                ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, lobbyVar);
                            }

                            break;
                        case "name":
                            RValue nameValue = new RValue(0);
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                            {
                                RValue nameVar = new RValue(0);
                                rnsReloaded.CreateString(&nameVar, "Archipelago name");
                                ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, nameVar);

                                rnsReloaded.CreateString(&nameValue, ArchipelagoName);
                            }
                            else
                            {
                                rnsReloaded.CreateString(&nameValue, originalName);
                            }

                            ModifyElementVariable(rnsReloaded, element, "defText", ModificationType.ModifyLiteral, nameValue);
                            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = nameValue;

                            break;
                        case "description":
                            RValue descValue = new RValue(0);
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                            {
                                RValue descVar = new RValue(0);
                                rnsReloaded.CreateString(&descVar, "Archipelago address");
                                ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, descVar);

                                rnsReloaded.CreateString(&descValue, ArchipelagoAddress);
                            }
                            else
                            {
                                rnsReloaded.CreateString(&descValue, originalDesc);
                            }
                            ModifyElementVariable(rnsReloaded, element, "defText", ModificationType.ModifyLiteral, descValue);
                            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = descValue;

                            break;
                        case "set password:":
                            RValue passValue = new RValue(0);
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                            {
                                RValue passVar = new RValue(0);
                                rnsReloaded.CreateString(&passVar, "enter password:");
                                ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, passVar);

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
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
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
                            ModifyElementVariable(rnsReloaded, element, "cursorPos", ModificationType.ModifyLiteral, passVal);
                            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2) = passVal;

                            RValue dummy = new RValue(0);
                            var passId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_passwordlock");
                            var passScript = rnsReloaded.GetScriptData(passId - 100000);
                            var createPasswordBoxHook = hooks.CreateHook<ScriptDelegate>(empty, passScript->Functions->Function);
                            createPasswordBoxHook!.OriginalFunction.Invoke(instance->Instance, other, &dummy, 0, argv);

                            break;
                        case "[ \"single player\",\"two players\",\"three players\",\"four players\" ]":
                            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                            {
                                ModifyElementVariable(rnsReloaded, element, "cursorPos", ModificationType.ModifyLiteral, new RValue(ArchipelagoNum - 1));
                                rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real = ArchipelagoNum;
                            }
                            else
                            {
                                ModifyElementVariable(rnsReloaded, element, "cursorPos", ModificationType.ModifyLiteral, new RValue(originalNum - 1));
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

            return returnValue;
        }

        // TODO: MAKE LOBBY SETTINGS NOT CHANGEABLE DURING RUNS
        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        internal RValue* UpdateLobbySettings(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.archipelagoOptionsReturnHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {
                ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                if (rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->Real == 1)
                {
                    ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));
                } else
                {
                    ArchipelagoPassword = "";
                }

                ArchipelagoNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
            }
            else
            {
                originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                if (rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->Real == 1)
                {
                    originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));
                }
                else
                {
                    originalPass = "";
                }

                originalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
            }
            return returnValue;
        }

        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        internal RValue* LobbyToTitle(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            RValue nameVal = new (0);
            rnsReloaded.CreateString(&nameVal, originalName);
            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = nameVal;

            RValue descVal = new (0);
            rnsReloaded.CreateString(&descVal, originalDesc);
            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = descVal;

            RValue passVal = new (0);
            rnsReloaded.CreateString(&passVal, originalPass);
            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3) = passVal;

            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4) = new RValue(originalNum);

            rnsReloaded.ExecuteScript("scr_online_save", null, null, []);

            returnValue = this.lobbyTitleHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;

        }

        // Change between displaying archieplago settings and regular settings
        internal RValue* UpdateLobbySettingsDisplayStep(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            FindLayer(rnsReloaded, "RunMenu_Options", out var layer);
            if (layer != null)
            {
                // Banner in the main lobby screen
                if (layer->Elements.Count == 8)
                {
                    // Update the text on the banner
                    FindElementInLayer(rnsReloaded, "click to edit", layer, out var lobbyButton);
                    if (lobbyButton != null)
                    {
                        if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                        {
                            RValue nameVar = new RValue(0);
                            rnsReloaded.CreateString(&nameVar, "click to edit archipelago settings");
                            ModifyElementVariable(rnsReloaded, lobbyButton, "name", ModificationType.ModifyLiteral, nameVar);

                        }
                        else if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 1 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 1 ||
                                rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 2 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 2
                        )
                        {
                            RValue lobbyVar = new RValue(0);
                            rnsReloaded.CreateString(&lobbyVar, "click to edit lobby settings");
                            ModifyElementVariable(rnsReloaded, lobbyButton, "name", ModificationType.ModifyLiteral, lobbyVar);

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
                        if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                        {
                            archipelagoPassSet = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->Real == 1;
                            ArchipelagoNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
                        }
                        else
                        {
                            originalPassSet = rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(2)->Real == 1;
                            originalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
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
                    layer->BeginScript.Real = -1;
                    this.lobbySettingsDisplayStepHook!.OriginalFunction(self, other, returnValue, argc, argv);

                    // Called as a layer step function, so we want to disable it once we leave the screens
                    this.lobbySettingsDisplayStepHook!.Disable();
                }

                return returnValue;
            }

            this.lobbySettingsDisplayStepHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Update the banner that displays the current lobby settings
        private bool UpdateBanner(RValue instanceValue, CLayerElementBase* element)
        {
            if (instanceValue.Get("diffTxt") != null &&
                rnsReloaded.GetString(instanceValue.Get("diffTxt")) == "[ \"CUTE\",\"NORMAL\",\"HARD\",\"LUNAR\" ]")
            {
                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                {
                    RValue nameVar = new RValue(0);
                    rnsReloaded.CreateString(&nameVar, ArchipelagoName);
                    ModifyElementVariable(rnsReloaded, element, "name", ModificationType.ModifyLiteral, nameVar);

                    RValue descVar = new RValue(0);
                    rnsReloaded.CreateString(&descVar, ArchipelagoAddress);
                    ModifyElementVariable(rnsReloaded, element, "descEdit", ModificationType.ModifyLiteral, descVar);

                    ModifyElementVariable(rnsReloaded, element, "maxPlayers", ModificationType.ModifyLiteral, new RValue(ArchipelagoNum));

                    ModifyElementVariable(rnsReloaded, element, "passwordLocked", ModificationType.ModifyLiteral, new RValue(archipelagoPassSet && ArchipelagoPassword != ""));
                }
                else
                {
                    RValue nameVar = new RValue(0);
                    rnsReloaded.CreateString(&nameVar, originalName);
                    ModifyElementVariable(rnsReloaded, element, "name", ModificationType.ModifyLiteral, nameVar);

                    RValue descVar = new RValue(0);
                    rnsReloaded.CreateString(&descVar, originalDesc);
                    ModifyElementVariable(rnsReloaded, element, "descEdit", ModificationType.ModifyLiteral, descVar);

                    ModifyElementVariable(rnsReloaded, element, "maxPlayers", ModificationType.ModifyLiteral, new RValue(originalNum));

                    ModifyElementVariable(rnsReloaded, element, "passwordLocked", ModificationType.ModifyLiteral, new RValue(originalPassSet && originalPass != ""));
                }
                return true;
            }
            return false;
        }

        // Update the lobby settings name
        internal RValue* UpdateLobbySettingsName(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.setNameHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {

                ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));
            }
            else
            {
                originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));
            }

            return returnValue;
        }

        // Update the lobby settings description
        internal RValue* UpdateLobbySettingsDesc(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.setDescHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {

                ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));
            }
            else
            {
                originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));
            }

            return returnValue;
        }

        // Update the lobby settings password
        internal RValue* UpdateLobbySettingsPass(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.setPassHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {

                ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
            }
            else
            {
                originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
            }

            return returnValue;
        }

    }
}
