using System.ComponentModel;

namespace RnSArchipelago.Config
{
    public unsafe class StartUpConfig
    {
        [DisplayName("Archipelago Name")]
        public string ArchipelagoName { get; set; } = "Player1";

        [DisplayName("Archipelago Address")]
        public string ArchipelagoAddress { get; set; } = "localhost:38281";

        [DisplayName("Archipelago Password")]
        [Description("WARNING! SHOWS IN PLAINTEXT\nEmpty means you have no password")]
        public string ArchipelagoPassword { get; set; } = "";

        [DisplayName("Skip ArchipelagoItem Folder Creation")]
        [Description("Enable only if you get an error on launch saying its unable find RnS file location\nWill need to manually add the ArchipelagoItems folder")]
        public bool SkipItemCreation { get; set; } = false;
    }
}
