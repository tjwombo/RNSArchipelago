using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using System.Runtime.InteropServices;
using RNSReloaded;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace RnSArchipelago.Utils
{
    internal unsafe static class HookUtil
    {
        internal enum ModificationType
        {
            ModifyLiteral,
            ModifyObject,
            ModifyArray,
            InsertToArray,
            DeleteFromArray
        }

        // Helper function to easily modify variables of an element
        internal static void ModifyElementVariable(IRNSReloaded rnsReloaded, CLayerElementBase* element, String variable, ModificationType modification, params RValue[] value)
        {

            var instance = (CLayerInstanceElement*)element;
            var instanceValue = new RValue(instance->Instance);
            RValue* objectToModify = instanceValue.Get(variable);

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
                case ModificationType.DeleteFromArray:
                    var args2 = new RValue[3];
                    args2[0] = *objectToModify;
                    args2[1] = value[0];
                    args2[2] = value[1];
                    rnsReloaded.ExecuteCodeFunction("array_delete", null, null, args2);
                    return;
                default:
                    return;
            }
        }

        // Find a given layer in the room
        internal static void FindLayer(IRNSReloaded rnsReloaded, string layerName, out CLayer* layer)
        {
            var room = rnsReloaded.GetCurrentRoom();
            // Find the layer in the room that contains the lobby type selector, RunMenu_Options
            layer = room->Layers.First;
            while (layer != null)
            {
                if (Marshal.PtrToStringAnsi((nint)layer->Name) == layerName)
                {
                    return;
                }
                layer = layer->Next;
            }
            layer = null;
            return;
        }

        // Given a layer, find an element
        internal static void FindElementInLayer(IRNSReloaded rnsReloaded, string elementIdentifier, CLayer* layer, out CLayerElementBase* element, string identifierField="name")
        {
            // Find the element in the layer that is the lobby type selector, has name lobby
            element = layer->Elements.First;
            while (element != null)
            {
                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                if (rnsReloaded.GetString(instanceValue.Get(identifierField)) == elementIdentifier)
                {
                    return;
                }
                element = element->Next;
            }
            element = null;
        }

        // Find an element on a given layer
        internal static void FindElementInLayer(IRNSReloaded rnsReloaded, string layerName, string elementIdentifier, out CLayerElementBase* element, string identifierField="name")
        {
            // Find the element in the layer that is the lobby type selector, has name lobby
            FindLayer(rnsReloaded, layerName, out var layer);
            if (layer != null)
            {
                element = layer->Elements.First;
                while (element != null)
                {
                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    if (rnsReloaded.GetString(instanceValue.Get(identifierField)) == elementIdentifier)
                    {
                        return;
                    }
                    element = element->Next;
                }
            }
            element = null;
        }

        // Find a layer and an element
        internal static void FindElementInLayer(IRNSReloaded rnsReloaded, string layerName, out CLayer* layer, string elementIdentifier, out CLayerElementBase* element, string identifierField="name")
        {
            // Find the element in the layer that is the lobby type selector, has name lobby
            FindLayer(rnsReloaded, layerName, out layer);
            if (layer != null)
            {
                element = layer->Elements.First;
                while (element != null)
                {
                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    if (rnsReloaded.GetString(instanceValue.Get(identifierField)) == elementIdentifier)
                    {
                        return;
                    }
                    element = element->Next;
                }
            }
            element = null;
        }

        // Helper function to find a layer with a given field, so we can use the other ones 
        internal static string FindLayerWithField(IRNSReloaded rnsReloaded, string field)
        {
            var room = rnsReloaded.GetCurrentRoom();
            // Find the layer in the room that contains the lobby type selector, RunMenu_Options
            var layer = room->Layers.First;
            while (layer != null)
            {
                // Find the element in the layer that is the lobby type selector, has name lobby
                var element = layer->Elements.First;
                while (element != null)
                {
                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    if (rnsReloaded.GetString(instanceValue.Get(field)) != null && rnsReloaded.GetString(instanceValue.Get(field)) != "unset")
                    {
                        return Marshal.PtrToStringAnsi((nint)layer->Name)!;
                    }
                    element = element->Next;
                }
                element = null;
                layer = layer->Next;
            }
            layer = null;
            return "";
        }
        // Return a string that contains information about the function that is getting hooked, namely the amount of arguments and their values
        internal static string PrintHook(IRNSReloaded rnsReloaded, string name, CInstance* self, RValue* returnValue, int argc, RValue** argv)
        {

            RValue a = new(self);
            var output = rnsReloaded.GetString(&a) + "\n";
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

        // An empty hook used when invoking a script isn't feasible, so we create a hook to invoke original function that way
        internal static RValue* empty(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            return returnValue;
        }
    }
}
