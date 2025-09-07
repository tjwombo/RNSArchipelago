using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace RnSArchipelago
{
    public unsafe class Mod : IMod
    {
        static Random random = new Random();

        private WeakReference<IRNSReloaded>? rnsReloadedRef;
        private WeakReference<IReloadedHooks>? hooksRef;
        private ILoggerV1 logger = null!;

        private IHook<ScriptDelegate>? outskirtsHook;

        private IHook<ScriptDelegate>? setItemHook;

        private IHook<ScriptDelegate>? setCharHook;

        private IHook<ScriptDelegate>? inventoryHook;

        private IHook<ScriptDelegate>? archipelagoButtonHook;

        private IHook<ScriptDelegate>? lobbySettingsDisplayHook;

        private IHook<ScriptDelegate>? archipelagoOptionsHook;

        private IHook<ScriptDelegate>? setNameHook;
        private IHook<ScriptDelegate>? setDescHook;
        private IHook<ScriptDelegate>? setPassHook;
        private IHook<ScriptDelegate>? setNumHook;

        private IHook<ScriptDelegate>? archipelagoOptionsReturnHook;

        private IHook<ScriptDelegate>? archipelagoWebsocketHook;

        private IHook<ScriptDelegate>? selectCharacterAbilitiesHook;
        private List<int> availablePrimary = new List<int>();
        private List<int> availableSecondary = new List<int>();
        private List<int> availableSpecial = new List<int>();
        private List<int> availableDefensive = new List<int>();

        private string archipelagoAddress = "archipelago.gg:12345";
        private string archipelagoName = "Player";
        private string archipelagoPassword = "";
        private int archipelagoNum = 4;


        private string originalName = "";
        private string originalDesc = "";
        private string originalPass = "";
        private int originalNum = 4;
        private bool returnUpdate = false;

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

        private enum ModificationType
        {
            ModifyLiteral,
            ModifyObject,
            ModifyArray,
            InsertToArray,
        }

        // Helper function to easily modify variables of an element
        private void ModifyElementVariable(CLayerElementBase* element, String variable, ModificationType modification, params RValue[] value)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);
                RValue* objectToModify = rnsReloaded.FindValue((&instanceValue)->Object, variable);

                switch (modification)
                {
                    case ModificationType.ModifyLiteral:
                        *objectToModify = value[0];
                        return;
                    case ModificationType.ModifyObject:
                        return;
                    case ModificationType.ModifyArray:
                        *objectToModify->Get(value[0].Int32) = value[1];
                        return;
                    case ModificationType.InsertToArray:
                        var args = new RValue[value.Length + 1];
                        Array.Copy(value, 0, args, 1, value.Length);
                        args[0] = *objectToModify;
                        rnsReloaded.ExecuteCodeFunction("array_push", null, null, args);
                        return;
                    default:
                        return;
                }
            }
        }

        // Set up the hooks relating to archipelago options
        private void AddArchipelagoButtonToMenu()
        {
            if (
                this.IsReady(out var rnsReloaded, out var hooks)
            )
            {
                var menuId = rnsReloaded.ScriptFindId("scr_runmenu_make_lobby_select");
                var menuScript = rnsReloaded.GetScriptData(menuId - 100000);
                this.archipelagoButtonHook = hooks.CreateHook<ScriptDelegate>(this.CreateArchipelagoLobbyType, menuScript->Functions->Function);
                this.archipelagoButtonHook.Activate();
                this.archipelagoButtonHook.Enable();
            }
        }

        // Modify the lobby types to have an archipelago option
        private RValue* CreateArchipelagoLobbyType(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            // Create the object
            returnValue = this.archipelagoButtonHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (this.IsReady(out var rnsReloaded))
            {
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

                                    if (rnsReloaded.utils.GetGlobalVar("obLobbyType")->Int32 == 3 || rnsReloaded.utils.GetGlobalVar("obLobbyType")->Real == 3)
                                    {
                                        ModifyElementVariable(element, "selectIndex", ModificationType.ModifyLiteral, new RValue(3));
                                    }

                                    return returnValue;
                                }
                                element = element->Next;
                            }  
                        }
                        layer = layer->Next;
                    }
                    
                }
            }
            return returnValue;
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
                this.archipelagoOptionsHook = hooks.CreateHook<ScriptDelegate>(this.CreateArchipelagoOptions, optionsScript->Functions->Function);
                this.archipelagoOptionsHook.Activate();
                this.archipelagoOptionsHook.Enable();

                var displayId = rnsReloaded.ScriptFindId("scr_runmenu_make_lobbysettings");
                var displayScript = rnsReloaded.GetScriptData(displayId - 100000);
                this.lobbySettingsDisplayHook = hooks.CreateHook<ScriptDelegate>(this.UpdateLobbySettingsDisplay, displayScript->Functions->Function);
                this.lobbySettingsDisplayHook.Activate();
                this.lobbySettingsDisplayHook.Enable();

                var nameId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_name");
                var nameScript = rnsReloaded.GetScriptData(nameId - 100000);
                this.setNameHook = hooks.CreateHook<ScriptDelegate>(this.UpdateLobbySettingsName, nameScript->Functions->Function);
                this.setNameHook.Activate();
                this.setNameHook.Enable();

                var descId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_set_desc");
                var descScript = rnsReloaded.GetScriptData(descId - 100000);
                this.setDescHook = hooks.CreateHook<ScriptDelegate>(this.UpdateLobbySettingsDesc, descScript->Functions->Function);
                this.setDescHook.Activate();
                this.setDescHook.Enable();

                var passId = rnsReloaded.ScriptFindId("textboxcomp_set_password");
                var passScript = rnsReloaded.GetScriptData(passId - 100000);
                this.setPassHook = hooks.CreateHook<ScriptDelegate>(this.UpdateLobbySettingsPass, passScript->Functions->Function);
                this.setPassHook.Activate();
                this.setPassHook.Enable();

                var returnId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_return");
                var returnScript = rnsReloaded.GetScriptData(returnId - 100000);
                this.archipelagoOptionsReturnHook = hooks.CreateHook<ScriptDelegate>(this.UpdateLobbySettings, returnScript->Functions->Function);
                this.archipelagoOptionsReturnHook.Activate();
                this.archipelagoOptionsReturnHook.Enable();
            }
        }

        // An empty hook used when invoking a script isn't feasible, so we create a hook to invoke original function that way
        private RValue* UpdateLobbySettingsDisplay(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("disp", returnValue, argc, argv), Color.Red);
            this.lobbySettingsDisplayHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // An empty hook used when invoking a script isn't feasible, so we create a hook to invoke original function that way
        private RValue* empty(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            return returnValue;
        }

        // Modify the archipelago lobby settings to display appropriate information
        private RValue* CreateArchipelagoOptions(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.archipelagoOptionsHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {
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
                                            ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, lobbyVar);

                                            break;
                                        case "name":
                                            RValue nameVar = new RValue(0);
                                            rnsReloaded.CreateString(&nameVar, "Archipelago name");
                                            ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, nameVar);

                                            RValue nameValue = new RValue(0);
                                            rnsReloaded.CreateString(&nameValue, archipelagoName);
                                            ModifyElementVariable(element, "defText", ModificationType.ModifyLiteral, nameValue);

                                            break;
                                        case "description":
                                            RValue descVar = new RValue(0);
                                            rnsReloaded.CreateString(&descVar, "Archipelago address");
                                            ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, descVar);

                                            RValue descValue = new RValue(0);
                                            rnsReloaded.CreateString(&descValue, archipelagoAddress);
                                            ModifyElementVariable(element, "defText", ModificationType.ModifyLiteral, descValue);

                                            break;
                                        case "set password:":
                                            RValue passVar = new RValue(0);
                                            rnsReloaded.CreateString(&passVar, "enter password:");
                                            ModifyElementVariable(element, "text", ModificationType.ModifyLiteral, passVar);

                                            RValue passValue = new RValue(0);
                                            rnsReloaded.CreateString(&passValue, archipelagoPassword);
                                            *rnsReloaded.utils.GetGlobalVar("lobbyPassword") = passValue;

                                            break;
                                        case "[ \"no password\",\"password locked\" ]":
                                            if (archipelagoPassword != "")
                                            {
                                                ModifyElementVariable(element, "cursorPos", ModificationType.ModifyLiteral, new RValue(1));
                                            } else
                                            {
                                                ModifyElementVariable(element, "cursorPos", ModificationType.ModifyLiteral, new RValue(0));
                                            }

                                            RValue dummy = new RValue(0);
                                            var passId = rnsReloaded.ScriptFindId("scr_runmenu_lobbysettings_passwordlock");
                                            var passScript = rnsReloaded.GetScriptData(passId - 100000);
                                            var createPasswordBoxHook = hooks.CreateHook<ScriptDelegate>(this.empty, passScript->Functions->Function);
                                            createPasswordBoxHook!.OriginalFunction.Invoke(instance->Instance, other, &dummy, 0, argv);

                                            break;
                                        case "[ \"single player\",\"two players\",\"three players\",\"four players\" ]":
                                            ModifyElementVariable(element, "cursorPos", ModificationType.ModifyLiteral, new RValue(archipelagoNum-1));
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
            }
            return returnValue;
        }

        // Update lobby settings such that archipelago and normal lobby settings are not coupled
        private RValue* UpdateLobbySettings(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            if (this.IsReady(out var rnsReloaded, out var hooks))
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
            returnValue = this.archipelagoOptionsReturnHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Update specifically the name
        private RValue* UpdateLobbySettingsName(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.setNameHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (this.IsReady(out var rnsReloaded, out var hooks))
            {

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
            }
            return returnValue;
        }

        // Update specifically the description/address
        private RValue* UpdateLobbySettingsDesc(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.setDescHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (this.IsReady(out var rnsReloaded, out var hooks))
            {

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
            }
            
            return returnValue;
        }

        // Update specifically the password
        private RValue* UpdateLobbySettingsPass(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            returnValue = this.setPassHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (this.IsReady(out var rnsReloaded, out var hooks))
            {
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
            }
            return returnValue;
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
            }
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

                    // Show error and return if connection problem


                    // Setup as if a friends only lobby or solo lobby based on the number of players
                    if (archipelagoNum > 1)
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

        // Prints information about the function that is getting hooked, namely the amount of arguments and their values
        private string PrintHook(string name, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.IsReady(out var rnsReloaded))
            {
                if (argc == 0)
                {
                    return $"{name}() -> {rnsReloaded.GetString(returnValue)}";
                }
                else
                {
                    var args = new List<string>();
                    var argsType = new List<string>();
                    for (var i = 0; i < argc; i++)
                    {
                        args.Add(rnsReloaded.GetString(argv[i]));
                        argsType.Add(argv[i]->Type.ToString());
                    }
                    return $"{name}({string.Join(", ", args)}) -> {rnsReloaded.GetString(returnValue)}";
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private RValue* CreateTestItem(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("item", returnValue, argc, argv), Color.Gray);
            returnValue = this.setItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        
        private RValue* CharTest(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("char", returnValue, argc, argv), Color.Gray);
            // Original function seems to attach the character abilities
            returnValue = this.setCharHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        private RValue* InventoryTest(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            this.logger.PrintMessage(this.PrintHook("inventory", returnValue, argc, argv), Color.Gray);
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
