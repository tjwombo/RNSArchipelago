using System.ComponentModel;
using static RnSArchipelago.Config.Config;

namespace RnSArchipelago.Config
{

    public unsafe class MessageConfig
    {
        [DisplayName("Show Archipelago System Message In Game")]
        public bool SystemLog { get; set; } = false;

        [DisplayName("Show Item Notifications")]
        public ItemType MessageSetting { get; set; } = ItemType.All;

        [DisplayName("Show Trap Item Notifications")]
        public bool TrapLog { get; set; } = true;

        [DisplayName("Show Other's Item Notifications")]
        [Description("Enable if you want to see logs where you are not the sender or reciever of the item")]
        public bool OtherLog { get; set; } = true;
    }
}
