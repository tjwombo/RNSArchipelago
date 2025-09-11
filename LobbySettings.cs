using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using static RnSArchipelago.Util;

namespace RnSArchipelago
{
    internal unsafe class LobbySettings
    {
        private IRNSReloaded rnsReloaded;
        private ILoggerV1 logger;
        private IReloadedHooks hooks;

        public IHook<ScriptDelegate>? archipelagoButtonHook;

        public IHook<ScriptDelegate>? lobbySettingsDisplayHook;
        public IHook<ScriptDelegate>? lobbySettingsDisplayStepHook;

        public IHook<ScriptDelegate>? archipelagoOptionsHook;

        public IHook<ScriptDelegate>? setNameHook;
        public IHook<ScriptDelegate>? setDescHook;
        public IHook<ScriptDelegate>? setPassHook;
        public IHook<ScriptDelegate>? setNumHook;
        public IHook<ScriptDelegate>? archipelagoOptionsReturnHook;

        public IHook<ScriptDelegate>? lobbyTitleHook;

        public string ArchipelagoAddress { get; private set; } = "archipelago.gg:12345";
        public string ArchipelagoName { get; private set; } = "Player";
        public string ArchipelagoPassword { get; private set; } = "";
        public int ArchipelagoNum { get; private set; } = 4;

        private string originalDesc = "";
        private string originalName = "";
        private string originalPass = "";
        private int originalNum = 4;
        private bool returnUpdate = false;

        private bool initialSetup = true;

        public LobbySettings(IRNSReloaded rnsReloaded, ILoggerV1 logger, IReloadedHooks hooks)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
            this.hooks = hooks;
        }

        // Modify the lobby types to have an archipelago option
        public RValue* CreateArchipelagoLobbyType(
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

            var room = rnsReloaded.GetCurrentRoom();
            if (room != null)
            {
                // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                var layer = room->Layers.First;
                while (layer != null)
                {
                    if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                    {
                        // Attach a layer script to hook into to act as a step hook
                        var stepId = rnsReloaded.CodeFunctionFind("os_get_info");
                        if (stepId.HasValue)
                        {
                            layer->BeginScript.Real = stepId.Value;
                            this.lobbySettingsDisplayStepHook!.Enable();
                        }

                        // Find the element in the layer that is the lobby type selector, has name lobby
                        var element = layer->Elements.First;
                        while (element != null)
                        {
                            var instance = (CLayerInstanceElement*)element;
                            var instanceValue = new RValue(instance->Instance);

                            if (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "name")) == "LOBBY")
                            {
                                Util.ModifyElementVariable(rnsReloaded, element, "nameXSc", Util.ModificationType.ModifyArray, [new RValue(1), new(0.75)]);
                                Util.ModifyElementVariable(rnsReloaded, element, "nameXSc", Util.ModificationType.InsertToArray, new RValue(0.75));

                                RValue nameValue = new RValue(0);
                                rnsReloaded.CreateString(&nameValue, "ARCHIPELAGO");
                                Util.ModifyElementVariable(rnsReloaded, element, "nameStr", Util.ModificationType.InsertToArray, nameValue);

                                RValue descValue = new RValue(0);
                                rnsReloaded.CreateString(&descValue, "lobby is open for archipelago");
                                Util.ModifyElementVariable(rnsReloaded, element, "descStr", Util.ModificationType.InsertToArray, descValue);

                                Util.ModifyElementVariable(rnsReloaded, element, "colorInd", Util.ModificationType.ModifyArray, [new RValue(3), new(8678193)]);

                                Util.ModifyElementVariable(rnsReloaded, element, "diffXPos", Util.ModificationType.ModifyArray, [new RValue(0), new(-210)]);
                                Util.ModifyElementVariable(rnsReloaded, element, "diffXPos", Util.ModificationType.ModifyArray, [new RValue(1), new(40)]);
                                Util.ModifyElementVariable(rnsReloaded, element, "diffXPos", Util.ModificationType.ModifyArray, [new RValue(2), new(290)]);
                                Util.ModifyElementVariable(rnsReloaded, element, "diffXPos", Util.ModificationType.InsertToArray, new RValue(540));

                                Util.ModifyElementVariable(rnsReloaded, element, "diffYPos", Util.ModificationType.InsertToArray, new RValue(-20));

                                Util.ModifyElementVariable(rnsReloaded, element, "maxIndex", Util.ModificationType.ModifyLiteral, new RValue(4));

                                Util.ModifyElementVariable(rnsReloaded, element, "selectionWidth", Util.ModificationType.ModifyLiteral, new RValue(250));

                                if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                                {
                                    Util.ModifyElementVariable(rnsReloaded, element, "selectIndex", Util.ModificationType.ModifyLiteral, new RValue(3));
                                }

                                return returnValue;
                            }
                            element = element->Next;
                        }
                    }
                    layer = layer->Next;
                }
            }
            return returnValue;
        }

        // Modify the archipelago lobby settings to display appropriate information
        public RValue* CreateArchipelagoOptions(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.archipelagoOptionsHook!.OriginalFunction(self, other, returnValue, argc, argv);

            var room = rnsReloaded.GetCurrentRoom();
            if (room != null)
            {
                // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                var layer = room->Layers.First;
                while (layer != null)
                {
                    if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                    {
                        // Find the element in the layer that is the lobby type selector, has name lobby
                        var element = layer->Elements.First;

                        CLayerElementBase* passwordBox = null;
                        while (element != null)
                        {
                            var instance = (CLayerInstanceElement*)element;
                            var instanceValue = new RValue(instance->Instance);

                            switch (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "text")))
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
                                    var createPasswordBoxHook = hooks.CreateHook<ScriptDelegate>(Util.empty, passScript->Functions->Function);
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

                    layer = layer->Next;
                }
            }

            return returnValue;
        }

        /*public RValue* UpdateLobbySettingsDisplay(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            *//*var room = rnsReloaded.GetCurrentRoom();
            if (room != null)
            {
                // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                var layer = room->Layers.First;
                while (layer != null)
                {
                    if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                    {
                        this.logger.PrintMessage("s" + layer->BeginScript.Real, Color.Red);
                        break;
                    }
                    layer = layer->Next;
                }
            }*//*


            this.logger.PrintMessage(Util.PrintHook(rnsReloaded, "disp", returnValue, argc, argv), Color.Red);
            this.lobbySettingsDisplayHook!.OriginalFunction(self, other, returnValue, argc, argv);
            this.logger.PrintMessage("c" + ArchipelagoNum + " " + originalNum, Color.Red);

            return returnValue;
        }

        public RValue* UpdateLobbySettingsDisplayStep(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            *//*var room = rnsReloaded.GetCurrentRoom();
            if (room != null)
            {
                // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                var layer = room->Layers.First;
                while (layer != null)
                {
                    if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                    {
                        if (layer->Elements.Count == 8)
                        {
                            var lobbyButton = layer->Elements.First;
                            while (lobbyButton != null)
                            {
                                var instance = (CLayerInstanceElement*)lobbyButton;
                                var instanceValue = new RValue(instance->Instance);

                                if (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "name")).StartsWith("click to edit"))
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

                                    break;
                                }


                                lobbyButton = lobbyButton->Next;
                            }


                            layer = room->Layers.First;
                            while (layer != null)
                            {
                                // Find the element in the layer that is the lobby display, has name lobby
                                var display = layer->Elements.First;
                                while (display != null)
                                {
                                    var instance = (CLayerInstanceElement*)display;
                                    var instanceValue = new RValue(instance->Instance);

                                    if (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "diffTxt")) == "[ \"CUTE\",\"NORMAL\",\"HARD\",\"LUNAR\" ]")
                                    {
                                        if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                                        {
                                            RValue nameVar = new RValue(0);
                                            rnsReloaded.CreateString(&nameVar, ArchipelagoName);
                                            ModifyElementVariable(rnsReloaded, display, "name", ModificationType.ModifyLiteral, nameVar);
                                            //currentName = ArchipelagoName;

                                            RValue descVar = new RValue(0);
                                            rnsReloaded.CreateString(&descVar, ArchipelagoAddress);
                                            ModifyElementVariable(rnsReloaded, display, "descEdit", ModificationType.ModifyLiteral, descVar);
                                            // currentDesc = ArchipelagoAddress;

                                            RValue passVar = new RValue(ArchipelagoPassword != "");
                                            ModifyElementVariable(rnsReloaded, display, "passwordLocked", ModificationType.ModifyLiteral, passVar);
                                            //currentPass = ArchipelagoPassword;

                                            RValue numVar = new RValue(ArchipelagoNum);
                                            ModifyElementVariable(rnsReloaded, display, "maxPlayers", ModificationType.ModifyLiteral, numVar);
                                            // currentNum = ArchipelagoNum;

                                            ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                                            ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                                            ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));

                                            ArchipelagoNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
                                        }
                                        else if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 1 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 1 ||
                                                rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 2 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 2
                                        )
                                        {
                                            RValue lobbyVar = new RValue(0);
                                            rnsReloaded.CreateString(&lobbyVar, OriginalName);
                                            ModifyElementVariable(rnsReloaded, display, "name", ModificationType.ModifyLiteral, lobbyVar);
                                            //currentName = originalName;

                                            RValue descVar = new RValue(0);
                                            rnsReloaded.CreateString(&descVar, OriginalDesc);
                                            ModifyElementVariable(rnsReloaded, display, "descEdit", ModificationType.ModifyLiteral, descVar);
                                            //currentDesc = originalDesc;

                                            RValue passVar = new RValue(OriginalPass != "");
                                            ModifyElementVariable(rnsReloaded, display, "passwordLocked", ModificationType.ModifyLiteral, passVar);
                                            //currentPass = originalPass;

                                            RValue numVar = new RValue(OriginalNum);
                                            ModifyElementVariable(rnsReloaded, display, "maxPlayers", ModificationType.ModifyLiteral, numVar);
                                            //currentNum = originalNum;

                                            OriginalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                                            OriginalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                                            OriginalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));

                                            OriginalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
                                        }

                                        return returnValue;
                                    }


                                    display = display->Next;
                                }
                                layer = layer->Next;
                            }

                            //this.lobbySettingsDisplayStepHook!.Disable();
                            return returnValue;
                        }
                        else
                        {
                            layer->BeginScript.Real = -1;
                            this.lobbySettingsDisplayStepHook!.OriginalFunction(self, other, returnValue, argc, argv);
                            this.lobbySettingsDisplayStepHook!.Disable();
                        }


                        return returnValue;
                    }
                    layer = layer->Next;
                }
            }*//*

            //this.logger.PrintMessage("why are we here", Color.Red);
            //this.scuffedLobbySettingsDisplayHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        */

        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        public RValue* UpdateLobbySettings(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.archipelagoOptionsReturnHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {

                ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));

                ArchipelagoNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;

            }
            else
            {

                originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));

                originalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
            }
            return returnValue;
        }

        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        public RValue* LobbyToTitle(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            RValue nameVal = new RValue(0);
            rnsReloaded.CreateString(&nameVal, originalName);
            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = nameVal;

            RValue descVal = new RValue(0);
            rnsReloaded.CreateString(&descVal, originalDesc);
            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = descVal;

            RValue passVal = new RValue(0);
            rnsReloaded.CreateString(&passVal, originalPass);
            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3) = passVal;

            *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4) = new RValue(originalNum);

            rnsReloaded.ExecuteScript("scr_online_save", null, null, []);

            returnValue = this.lobbyTitleHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;

        }

        public RValue* UpdateLobbySettingsDisplayStep(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            /*var room = rnsReloaded.GetCurrentRoom();
            if (room != null)
            {
                // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                var layer = room->Layers.First;
                while (layer != null)
                {
                    if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                    {
                        if (layer->Elements.Count == 8)
                        {
                            var lobbyButton = layer->Elements.First;
                            while (lobbyButton != null)
                            {
                                var instance = (CLayerInstanceElement*)lobbyButton;
                                var instanceValue = new RValue(instance->Instance);

                                if (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "name")).StartsWith("click to edit"))
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

                                    break;
                                }


                                lobbyButton = lobbyButton->Next;
                            }


                            layer = room->Layers.First;
                            while (layer != null)
                            {
                                // Find the element in the layer that is the lobby display, has name lobby
                                var display = layer->Elements.First;
                                while (display != null)
                                {
                                    var instance = (CLayerInstanceElement*)display;
                                    var instanceValue = new RValue(instance->Instance);

                                    if (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "diffTxt")) == "[ \"CUTE\",\"NORMAL\",\"HARD\",\"LUNAR\" ]")
                                    {
                                        if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                                        {
                                            RValue nameVar = new RValue(0);
                                            rnsReloaded.CreateString(&nameVar, ArchipelagoName);
                                            ModifyElementVariable(rnsReloaded, display, "name", ModificationType.ModifyLiteral, nameVar);
                                            //currentName = ArchipelagoName;

                                            RValue descVar = new RValue(0);
                                            rnsReloaded.CreateString(&descVar, ArchipelagoAddress);
                                            ModifyElementVariable(rnsReloaded, display, "descEdit", ModificationType.ModifyLiteral, descVar);
                                            // currentDesc = ArchipelagoAddress;

                                            RValue passVar = new RValue(ArchipelagoPassword != "");
                                            ModifyElementVariable(rnsReloaded, display, "passwordLocked", ModificationType.ModifyLiteral, passVar);
                                            //currentPass = ArchipelagoPassword;

                                            RValue numVar = new RValue(ArchipelagoNum);
                                            ModifyElementVariable(rnsReloaded, display, "maxPlayers", ModificationType.ModifyLiteral, numVar);
                                            // currentNum = ArchipelagoNum;

                                            ArchipelagoName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                                            ArchipelagoAddress = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                                            ArchipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));

                                            ArchipelagoNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
                                        }
                                        else if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 1 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 1 ||
                                                rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 2 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 2
                                        )
                                        {
                                            RValue lobbyVar = new RValue(0);
                                            rnsReloaded.CreateString(&lobbyVar, originalName);
                                            ModifyElementVariable(rnsReloaded, display, "name", ModificationType.ModifyLiteral, lobbyVar);
                                            //currentName = originalName;

                                            RValue descVar = new RValue(0);
                                            rnsReloaded.CreateString(&descVar, originalDesc);
                                            ModifyElementVariable(rnsReloaded, display, "descEdit", ModificationType.ModifyLiteral, descVar);
                                            //currentDesc = originalDesc;

                                            RValue passVar = new RValue(originalPass != "");
                                            ModifyElementVariable(rnsReloaded, display, "passwordLocked", ModificationType.ModifyLiteral, passVar);
                                            //currentPass = originalPass;

                                            RValue numVar = new RValue(originalNum);
                                            ModifyElementVariable(rnsReloaded, display, "maxPlayers", ModificationType.ModifyLiteral, numVar);
                                            //currentNum = originalNum;

                                            originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));

                                            originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));

                                            originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(3));

                                            originalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;
                                        }

                                        return returnValue;
                                    }


                                    display = display->Next;
                                }
                                layer = layer->Next;
                            }

                            //this.lobbySettingsDisplayStepHook!.Disable();
                            return returnValue;
                        }
                        else
                        {
                            layer->BeginScript.Real = -1;
                            this.lobbySettingsDisplayStepHook!.OriginalFunction(self, other, returnValue, argc, argv);
                            this.lobbySettingsDisplayStepHook!.Disable();
                        }


                        return returnValue;
                    }
                    layer = layer->Next;
                }
            }*/

            var room = rnsReloaded.GetCurrentRoom();
            if (room != null)
            {
                // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                var layer = room->Layers.First;
                while (layer != null)
                {
                    if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                    {
                        if (layer->Elements.Count == 8)
                        {
                            var lobbyButton = layer->Elements.First;
                            while (lobbyButton != null)
                            {
                                var instance = (CLayerInstanceElement*)lobbyButton;
                                var instanceValue = new RValue(instance->Instance);

                                if (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "name")).StartsWith("click to edit"))
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

                                    break;
                                }


                                lobbyButton = lobbyButton->Next;
                            }


                            // Find the element in the layer that is the lobby display, has name lobby
                            layer = room->Layers.First;
                            while (layer != null)
                            {
                                var display = layer->Elements.First;
                                while (display != null)
                                {
                                    var instance = (CLayerInstanceElement*)display;
                                    var instanceValue = new RValue(instance->Instance);

                                    if (rnsReloaded.FindValue((&instanceValue)->Object, "diffTxt") != null &&
                                        rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "diffTxt")) == "[ \"CUTE\",\"NORMAL\",\"HARD\",\"LUNAR\" ]")
                                    {
                                        if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                                        {
                                            RValue nameVar = new RValue(0);
                                            rnsReloaded.CreateString(&nameVar, ArchipelagoName);
                                            ModifyElementVariable(rnsReloaded, display, "name", ModificationType.ModifyLiteral, nameVar);

                                            RValue descVar = new RValue(0);
                                            rnsReloaded.CreateString(&descVar, ArchipelagoAddress);
                                            ModifyElementVariable(rnsReloaded, display, "descEdit", ModificationType.ModifyLiteral, descVar);

                                            ModifyElementVariable(rnsReloaded, display, "maxPlayers", ModificationType.ModifyLiteral, new RValue(ArchipelagoNum));

                                            ModifyElementVariable(rnsReloaded, display, "passwordLocked", ModificationType.ModifyLiteral, new RValue(ArchipelagoPassword != ""));
                                        } else
                                        {
                                            RValue nameVar = new RValue(0);
                                            rnsReloaded.CreateString(&nameVar, originalName);
                                            ModifyElementVariable(rnsReloaded, display, "name", ModificationType.ModifyLiteral, nameVar);

                                            RValue descVar = new RValue(0);
                                            rnsReloaded.CreateString(&descVar, originalDesc);
                                            ModifyElementVariable(rnsReloaded, display, "descEdit", ModificationType.ModifyLiteral, descVar);

                                            ModifyElementVariable(rnsReloaded, display, "maxPlayers", ModificationType.ModifyLiteral, new RValue(originalNum));

                                            ModifyElementVariable(rnsReloaded, display, "passwordLocked", ModificationType.ModifyLiteral, new RValue(originalPass != ""));
                                        }

                                        return returnValue;
                                    }

                                    display = display->Next;

                                }

                                layer = layer->Next;
                            }

                            //this.lobbySettingsDisplayStepHook!.Disable();
                            return returnValue;
                        }
                        else
                        {
                            layer->BeginScript.Real = -1;
                            this.lobbySettingsDisplayStepHook!.OriginalFunction(self, other, returnValue, argc, argv);
                            this.lobbySettingsDisplayStepHook!.Disable();
                        }


                        return returnValue;
                    }
                    layer = layer->Next;
                }
            }


            //this.logger.PrintMessage("why are we here", Color.Red);
            //this.scuffedLobbySettingsDisplayHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }
    }
}
