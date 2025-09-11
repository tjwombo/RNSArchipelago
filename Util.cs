using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago
{
    internal unsafe static class Util
    {
        public enum ModificationType
        {
            ModifyLiteral,
            ModifyObject,
            ModifyArray,
            InsertToArray,
        }

        // Helper function to easily modify variables of an element
        public static  void ModifyElementVariable(IRNSReloaded rnsReloaded, CLayerElementBase* element, String variable, ModificationType modification, params RValue[] value)
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

        // Prints information about the function that is getting hooked, namely the amount of arguments and their values
        public static string PrintHook(IRNSReloaded rnsReloaded, string name, RValue* returnValue, int argc, RValue** argv)
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

        // An empty hook used when invoking a script isn't feasible, so we create a hook to invoke original function that way
        public static RValue* empty(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        )
        {
            return returnValue;
        }
    }
}
