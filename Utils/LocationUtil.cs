using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Utils
{
    internal unsafe static class LocationUtil
    {
       internal static WeakReference<IRNSReloaded> rnsReloadedRef = null!;

       internal enum LocationType
        {
            Other,
            Start,
            Battle,
            Boss,
            Chest,
            SpecialChest,
            Shop
        }

        // Get the location type of the provided notch index, default is current notch
        internal static LocationType GetLocationType(int index = -1)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "xSubimg", out var element);
                var instance = ((CLayerInstanceElement*)element)->Instance;

                if (index == -1)
                {
                    index = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "currentPos"));
                }

                var currentXImg = rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(instance, "xSubimg"), index);

                if (HookUtil.IsEqualToNumeric(currentXImg, 1))
                {
                    return LocationType.Chest;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 2))
                {
                    return LocationType.Shop;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 4))
                {
                    return LocationType.Boss;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 5))
                {
                    return LocationType.SpecialChest;
                }
                else if (HookUtil.IsEqualToNumeric(currentXImg, 0))
                {
                    return LocationType.Battle;
                }
            }
            return LocationType.Other;
        }

        // Get the name of the location for the notch based on its image and number of occurence
        internal static string GetNotchName(CInstance* element)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var notchPos = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "currentPos"));
                var notchType = GetLocationType();

                if (notchType == LocationType.Boss)
                {
                    return " Boss";
                }

                int count = 1;

                var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
                kingdomName = kingdomName.Replace(Environment.NewLine, " ");


                for (var i = kingdomName.Equals("Kingdom Outskirts") || kingdomName.Equals("Crack in the Geode") ? 1 : 0; i < notchPos; i++)
                {
                    if (GetLocationType(i) == notchType)
                    {
                        count++;
                    }
                }

                if (notchType == LocationType.Chest)
                {
                    return " Chest " + count;
                }

                if (notchType == LocationType.Battle)
                {
                    return " Battle " + count;
                }

                if (notchType == LocationType.Shop)
                {
                    return " Shop";
                }
            }

            return "";
        }

        // Get the base location name, ex. Kingdom Outskirts Chest 1
        internal static string GetBaseLocation()
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                HookUtil.FindElementInLayer("RunMenu_Blocker", "currentPos", out var instance);
                var element = ((CLayerInstanceElement*)instance)->Instance;

                var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
                kingdomName = kingdomName.Replace(Environment.NewLine, " ");

                var notchName = GetNotchName(element);
                if (notchName.Contains("Chest") && !kingdomName.Equals("Kingdom Outskirts") && !kingdomName.Equals("Crack in the Geode"))
                {
                    notchName = " Chest";
                }
                else if (kingdomName.Equals("Moonlit Pinnacle"))
                {
                    return "Shira";
                }
                else if (kingdomName.Equals("Reflecting Pool"))
                {
                    return "Witch";
                }

                return kingdomName + notchName;
            }
            return "";
        }
    }
}
