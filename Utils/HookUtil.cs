using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Reloaded.Mod.Interfaces;

namespace RnSArchipelago.Utils
{
    internal unsafe class HookUtil
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        
        public HookUtil(WeakReference<IRNSReloaded> rnsReloadedRef, ILogger logger)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
        }
        
        internal enum ModificationType
        {
            ModifyLiteral,
            ModifyObject,
            ModifyArray,
            InsertToArray,
            DeleteFromArray
        }

        // Helper function to easily modify variables of an element
        internal void ModifyElementVariable(CLayerElementBase* element, String variable, ModificationType modification, params RValue[] value)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;
            
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

        internal RValue CreateRArray(object[] values)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return null;
            
            var array = rnsReloaded.ExecuteCodeFunction("array_create", null, null, [new(values.Length)]);
            if (array.HasValue)
            {
                var arrayValues = array.Value;
                for (var i = 0; i < values.Length; i++)
                {
                    if (values[i] is string s)
                    {
                        var stringValue = new RValue();
                        rnsReloaded.CreateString(&stringValue, s);
                        *arrayValues[i] = stringValue;

                    }
                    else if (values[i] is long l)
                    {
                        *arrayValues[i] = new RValue(l);
                    } else if (values[i] is double d)
                    {
                        *arrayValues[i] = new RValue(d);
                    } 
                    else if (values[i] is int intValue)
                    {
                        *arrayValues[i] = new RValue(intValue);
                    } 
                    else
                    {
                        logger.PrintMessage(values[i] + " is not convertable", System.Drawing.Color.Red);
                    }
                }

                return arrayValues;
            }
            return null;
        }

        internal bool IsEqualToNumeric(RValue* value, long testValue)
        {
            return value->Real == testValue || value->Int32 == testValue;
        }

        internal long GetNumeric(RValue* value)
        {
            return (int)value->Real | value->Int32;
        }

        internal long GetNumeric(RValue value)
        {
            return (int)value.Real | value.Int32;
        }

        // Find a given layer in the room
        internal void FindLayer(string layerName, out CLayer* layer)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                layer = null;
                return;
            }

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
        }

        // Given a layer, find an element
        internal void FindElementInLayer(string variableName, string variableValue, CLayer* layer, out CLayerElementBase* element)
        {
            element = null;
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Find the element in the layer that is the lobby type selector, has name lobby
            element = layer->Elements.First;
            while (element != null)
            {
                var instance = (CLayerInstanceElement*)element;
                var instanceValue = new RValue(instance->Instance);

                if (rnsReloaded.GetString(instanceValue.Get(variableName)) == variableValue)
                {
                    return;
                }

                element = element->Next;
            }
        }

        // Find an element on a given layer with a specific value
        internal void FindElementInLayer(string layerName, string variableName, string variableValue, out CLayerElementBase* element)
        {
            element = null;
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;

            // Find the element in the layer that is the lobby type selector, has name lobby
            FindLayer(layerName, out var layer);
            if (layer != null)
            {
                element = layer->Elements.First;
                while (element != null)
                {
                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    if (rnsReloaded.GetString(instanceValue.Get(variableName)) == variableValue)
                    {
                        return;
                    }

                    element = element->Next;
                }
            }
        }

        // Find an element on a given layer
        internal void FindElementInLayer(string layerName, string variableName, out CLayerElementBase* element)
        {
            element = null;
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;
            
            // Find the element in the layer that is the lobby type selector, has name lobby
            FindLayer(layerName, out var layer);
            if (layer != null)
            {
                element = layer->Elements.First;
                while (element != null)
                {
                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    if (instanceValue.Get(variableName) != null && instanceValue.Get(variableName)->ToString() != "unset")
                    {
                        return;
                    }
                    element = element->Next;
                }
            }
        }

        // Find a layer and an element
        internal void FindElementInLayer(string layerName, out CLayer* layer, string variableName, string variableValue, out CLayerElementBase* element)
        {
            layer = null;
            element = null;
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;
            
            // Find the element in the layer that is the lobby type selector, has name lobby
            FindLayer(layerName, out layer);
            if (layer != null)
            {
                element = layer->Elements.First;
                while (element != null)
                {
                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    if (rnsReloaded.GetString(instanceValue.Get(variableName)) == variableValue)
                    {
                        return;
                    }
                    element = element->Next;
                }
            }
        }

        // Helper function to find a layer with a given field, so we can use the other ones 
        internal string FindLayerWithField(string field)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return "";
            
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
                layer = layer->Next;
            }
            return "";
        }
        // Helper function to find a layer with a given field and value, so we can use the other ones 
        internal string FindLayerWithField(string field, string value)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return "";
            
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

                    if (rnsReloaded.GetString(instanceValue.Get(field)) != null && rnsReloaded.GetString(instanceValue.Get(field)) != "unset"
                        && rnsReloaded.GetString(instanceValue.Get(field)) == value)
                    {
                        return Marshal.PtrToStringAnsi((nint)layer->Name)!;
                    }
                    element = element->Next;
                }
                layer = layer->Next;
            }
            return "";
        }
        // Find an element knowing only a field name and its expected value
        internal void FindElement(string field, string value, out CLayerElementBase* element)
        {
            element = null;
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return;
            
            var room = rnsReloaded.GetCurrentRoom();
            // Find the layer in the room that contains the lobby type selector, RunMenu_Options
            var layer = room->Layers.First;
            while (layer != null)
            {
                // Find the element in the layer that is the lobby type selector, has name lobby
                element = layer->Elements.First;
                while (element != null)
                {
                    var instance = (CLayerInstanceElement*)element;
                    var instanceValue = new RValue(instance->Instance);

                    if (rnsReloaded.GetString(instanceValue.Get(field)) != null && rnsReloaded.GetString(instanceValue.Get(field)) != "unset"
                        && rnsReloaded.GetString(instanceValue.Get(field)) == value)
                    {
                        return;
                    }
                    element = element->Next;
                }
                element = null;
                layer = layer->Next;
            }
        }
        // Return a string that contains information about the function that is getting hooked, namely the amount of arguments and their values
        internal string PrintHook(string name, CInstance* self, RValue* returnValue, int argc, RValue** argv)
        {
            if (!rnsReloadedRef.TryGetTarget(out var rnsReloaded)) return $"Error in calling: {name}";
            
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
