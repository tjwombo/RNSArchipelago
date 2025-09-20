using Archipelago.MultiClient.Net.Packets;
using RnSArchipelago.Connection;
using RnSArchipelago.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RnSArchipelago.Utils
{
    internal class InventoryUtil
    {
        private static readonly InventoryUtil _instance = new InventoryUtil();

        internal static InventoryUtil Instance => _instance;

        private InventoryUtil() {
            AvailableKingdoms = KingdomFlags.None;
            ProgressiveKingdoms = 0;
        }

        [Flags]
        internal enum KingdomFlags
        { 
            None = 0b00000000,
            Outskirts = 0b00000001,
            Scholars_Nest = 0b00000010,
            Kings_Arsenal = 0b00000100,
            Red_Darkhouse = 0b00001000,
            Churchmouse_Streets = 0b00010000,
            Emerald_Lakeside = 0b00100000,
            The_Pale_Keep = 0b01000000,
            Moonlit_Pinnacle = 0b10000000,
            All = 0b11111111
        }

        private static readonly string[] KINGDOMS = ["Kingdom Outskirts", "Scholar's Nest", "King's Arsenal", "Red Darkhouse", "Churchmouse Streets", "Emerald Lakeside", "The Pale Keep", "Moonlit Pinnacle"];

        internal KingdomFlags AvailableKingdoms { get; set; }
        internal int ProgressiveKingdoms { get; set; }

        internal void ReceiveItem(ReceivedItemsPacket recievedItem, SharedData data)
        {
            foreach (var item in recievedItem.Items)
            {
                var itemName = data.GetValue<string>(DataContext.IdToItem, item.Item);
                if (itemName != default)
                {
                    if (KINGDOMS.Contains(itemName))
                    {
                        AvailableKingdoms = AvailableKingdoms | (KingdomFlags)Enum.Parse(typeof(KingdomFlags), itemName.Replace(" ", "_").Replace("'", ""));
                    }
                }
                
            }
        }

        internal int AvailableKingdomsCount()
        {
            int count = 0;
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Scholars_Nest) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Kings_Arsenal) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Emerald_Lakeside) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Churchmouse_Streets) != 0)
            {
                count++;
            }
            if ((AvailableKingdoms & InventoryUtil.KingdomFlags.Red_Darkhouse) != 0)
            {
                count++;
            }
            return count;
        }

        internal void ResetItems()
        {
            AvailableKingdoms = KingdomFlags.None;
            ProgressiveKingdoms = 0;
        }

    }
}
