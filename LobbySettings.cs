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
        public IHook<ScriptDelegate>? scuffedLobbySettingsDisplayHook;

        public IHook<ScriptDelegate>? archipelagoOptionsHook;

        public IHook<ScriptDelegate>? setNameHook;
        public IHook<ScriptDelegate>? setDescHook;
        public IHook<ScriptDelegate>? setPassHook;
        public IHook<ScriptDelegate>? setNumHook;

        public IHook<ScriptDelegate>? archipelagoOptionsReturnHook;

        private string archipelagoAddress = "archipelago.gg:12345";
        private string archipelagoName = "Player";
        private string archipelagoPassword = "";
        private int archipelagoNum = 4;


        private string originalName = "";
        private string originalDesc = "";
        private string originalPass = "";
        private int originalNum = 4;
        private bool returnUpdate = false;

        public int ArchipelagoNum
        { get; }

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

            originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(0));
            originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(1));
            originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
            originalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettings")->Get(4)->Real;

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

            // If the lobby type is archipelago set up the websocket
            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {
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
                                        RValue lobbyVar = new RValue(0);
                                        rnsReloaded.CreateString(&lobbyVar, "ARCHIPELAGO SETTINGS");
                                        ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, lobbyVar);

                                        break;
                                    case "name":
                                        RValue nameVar = new RValue(0);
                                        rnsReloaded.CreateString(&nameVar, "Archipelago name");
                                        ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, nameVar);

                                        RValue nameValue = new RValue(0);
                                        rnsReloaded.CreateString(&nameValue, archipelagoName);
                                        ModifyElementVariable(rnsReloaded, element, "defText", ModificationType.ModifyLiteral, nameValue);

                                        break;
                                    case "description":
                                        RValue descVar = new RValue(0);
                                        rnsReloaded.CreateString(&descVar, "Archipelago address");
                                        ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, descVar);

                                        RValue descValue = new RValue(0);
                                        rnsReloaded.CreateString(&descValue, archipelagoAddress);
                                        ModifyElementVariable(rnsReloaded, element, "defText", ModificationType.ModifyLiteral, descValue);

                                        break;
                                    case "set password:":
                                        RValue passVar = new RValue(0);
                                        rnsReloaded.CreateString(&passVar, "enter password:");
                                        ModifyElementVariable(rnsReloaded, element, "text", ModificationType.ModifyLiteral, passVar);

                                        RValue passValue = new RValue(0);
                                        rnsReloaded.CreateString(&passValue, archipelagoPassword);
                                        *rnsReloaded.utils.GetGlobalVar("lobbyPassword") = passValue;

                                        break;
                                    case "[ \"no password\",\"password locked\" ]":
                                        if (archipelagoPassword != "")
                                        {
                                            ModifyElementVariable(rnsReloaded, element, "cursorPos", ModificationType.ModifyLiteral, new RValue(1));
                                        }
                                        else
                                        {
                                            ModifyElementVariable(rnsReloaded, element, "cursorPos", ModificationType.ModifyLiteral, new RValue(0));
                                        }

                                        RValue dummy = new RValue(0);
                                        var passId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_passwordlock");
                                        var passScript = rnsReloaded.GetScriptData(passId - 100000);
                                        var createPasswordBoxHook = hooks.CreateHook<ScriptDelegate>(Util.empty, passScript->Functions->Function);
                                        createPasswordBoxHook!.OriginalFunction.Invoke(instance->Instance, other, &dummy, 0, argv);

                                        break;
                                    case "[ \"single player\",\"two players\",\"three players\",\"four players\" ]":
                                        ModifyElementVariable(rnsReloaded, element, "cursorPos", ModificationType.ModifyLiteral, new RValue(archipelagoNum - 1));
                                        rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real = archipelagoNum;
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
            }
            return returnValue;
        }

        public RValue* UpdateLobbySettingsDisplay(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {

            var room = rnsReloaded.GetCurrentRoom();
            if (room != null)
            {
                // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                var layer = room->Layers.First;
                while (layer != null)
                {
                    if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                    {
                        this.logger.PrintMessage("" + layer->BeginScript.Real, Color.Red);
                        if (layer->BeginScript.Real == -1)
                        {
                            var osId = rnsReloaded.CodeFunctionFind("os_get_info");
                            if (osId.HasValue)
                            {
                                layer->BeginScript.Real = osId.Value;
                            }
                            /*//var func = hooks.CreateFunction
                            ulong addr = 8056L;
                            test test3 = test2;
                            var func = hooks.CreateFunctionPtr<test>(addr);
                            var addr2 = hooks.Utilities.WritePointer(8056);
                            //addr2.ToPointer = test3;
                            //func
                            RValue test = new();
                            test.Pointer = func.GetFunctionAddress();
                            rnsReloaded.ExecuteCodeFunction("layer_script_begin", null, null, [new(layer->ID), test]);
                            this.logger.PrintMessage("" + layer->BeginScript.Real, Color.Red);*/
                        }
                        //layer->BeginScript
                        break;
                    }
                    layer = layer->Next;
                }
                this.logger.PrintMessage("end", Color.Red);
            }
            else
            {
                this.logger.PrintMessage("bad", Color.Red);
            }

            this.logger.PrintMessage(Util.PrintHook(rnsReloaded, "disp", returnValue, argc, argv), Color.Red);
            this.lobbySettingsDisplayHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        public RValue* UpdateLobbySettings(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {

            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {

                RValue originalPassVal = new RValue(0);
                rnsReloaded.CreateString(&originalPassVal, originalPass);
                *rnsReloaded.utils.GetGlobalVar("lobbyPassword") = originalPassVal;

                archipelagoNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;

                returnUpdate = true;
                returnValue = this.archipelagoOptionsReturnHook!.OriginalFunction(self, other, returnValue, argc, argv);
                returnUpdate = false;

                *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4) = new RValue(originalNum);
            }
            else
            {
                originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));

                originalNum = (int)rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(4)->Real;

                returnValue = this.archipelagoOptionsReturnHook!.OriginalFunction(self, other, returnValue, argc, argv);
            }
            return returnValue;
        }

        // Update specifically the name
        public RValue* UpdateLobbySettingsName(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.setNameHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {
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
                            while (element != null)
                            {
                                var instance = (CLayerInstanceElement*)element;
                                var instanceValue = new RValue(instance->Instance);

                                switch (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "text")))
                                {
                                    case "Archipelago name":
                                        archipelagoName = rnsReloaded.GetString(rnsReloaded.FindValue(instanceValue.Object, "defText"));
                                        RValue originalNameVal = new RValue(0);
                                        rnsReloaded.CreateString(&originalNameVal, originalName);
                                        *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0) = originalNameVal;

                                        return returnValue;
                                    default:
                                        break;
                                }

                                element = element->Next;
                            }

                        }
                        layer = layer->Next;
                    }
                }
            }
            else
            {
                originalName = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(0));
            }
            return returnValue;
        }

        // Update specifically the description/address
        public RValue* UpdateLobbySettingsDesc(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.setDescHook!.OriginalFunction(self, other, returnValue, argc, argv);


            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {
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
                            while (element != null)
                            {
                                var instance = (CLayerInstanceElement*)element;
                                var instanceValue = new RValue(instance->Instance);

                                switch (rnsReloaded.GetString(rnsReloaded.FindValue((&instanceValue)->Object, "text")))
                                {
                                    case "Archipelago address":
                                        archipelagoAddress = rnsReloaded.GetString(rnsReloaded.FindValue(instanceValue.Object, "defText"));
                                        RValue originalDescVal = new RValue(0);
                                        rnsReloaded.CreateString(&originalDescVal, originalDesc);
                                        *rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1) = originalDescVal;

                                        return returnValue;
                                    default:
                                        break;
                                }

                                element = element->Next;
                            }

                        }
                        layer = layer->Next;
                    }
                }
            }
            else
            {
                originalDesc = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbySettingsDef")->Get(1));
            }
            return returnValue;
        }

        // Update specifically the password
        public RValue* UpdateLobbySettingsPass(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.setPassHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
            {
                archipelagoPassword = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));

                // If the update is because of leaving, instead of entering the textbox, we need to restore the original password back
                if (returnUpdate)
                {
                    RValue originalPassVal = new RValue(0);
                    rnsReloaded.CreateString(&originalPassVal, originalPass);
                    *rnsReloaded.utils.GetGlobalVar("lobbyPassword") = originalPassVal;
                }
            }
            else
            {
                originalPass = rnsReloaded.GetString(rnsReloaded.utils.GetGlobalVar("lobbyPassword"));
            }
            return returnValue;
        }

        public RValue* UpdateLobbySettingsDisplayOsRedirect(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            /*if (this.IsReady(out var rnsReloaded, out var hooks))
            {
                var room = rnsReloaded.GetCurrentRoom();
                if (room != null)
                {
                    // Find the layer in the room that contains the lobby type selector, RunMenu_Options
                    var layer = room->Layers.First;
                    while (layer != null)
                    {
                        if (Marshal.PtrToStringAnsi((nint)layer->Name) == "RunMenu_Options")
                        {
                            this.logger.PrintMessage("" + layer->BeginScript.Real, Color.Red);
                            if (layer->BeginScript.Real == -1)
                            {
                                var osId = rnsReloaded.CodeFunctionFind("os_get_info");
                                if (osId.HasValue)
                                {
                                    layer->BeginScript.Real = osId.Value;
                                }
                                *//*//var func = hooks.CreateFunction
                                ulong addr = 8056L;
                                test test3 = test2;
                                var func = hooks.CreateFunctionPtr<test>(addr);
                                var addr2 = hooks.Utilities.WritePointer(8056);
                                //addr2.ToPointer = test3;
                                //func
                                RValue test = new();
                                test.Pointer = func.GetFunctionAddress();
                                rnsReloaded.ExecuteCodeFunction("layer_script_begin", null, null, [new(layer->ID), test]);
                                this.logger.PrintMessage("" + layer->BeginScript.Real, Color.Red);*//*
                            }
                            //layer->BeginScript
                            break;
                        }
                        layer = layer->Next;
                    }
                    this.logger.PrintMessage("end", Color.Red);
                }
                else
                {
                    this.logger.PrintMessage("bad", Color.Red);
                }
            }
            this.logger.PrintMessage("why are we here", Color.Red);*/
            //this.scuffedLobbySettingsDisplayHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }
    }
}
