using System.ComponentModel;
using static RnSArchipelago.Config.Config;

namespace RnSArchipelago.Config
{

    public unsafe class HintConfig
    {
        [DisplayName("Create Hints For Item Types")]
        public ItemType HintSetting { get; set; } = ItemType.All;

        [DisplayName("Show Archipelago Hints Message In Game")]
        public bool HintLog { get; set; } = false;
    }
}
