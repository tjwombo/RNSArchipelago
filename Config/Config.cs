using System.ComponentModel;

namespace RnSArchipelago.Config;

public class Config : Configurable<Config>
{
    [DisplayName("Archipelago Name")]
    public string ArchipelagoName { get; set; } = "Player1";

    [DisplayName("Archipelago Address")]
    public string ArchipelagoAddress { get; set; } = "localhost:38281";

    [DisplayName("Enable Extra Debug Messages")]
    public bool ExtraDebugMessages { get; set; } = false;

    [DisplayName("Skip ArchipelagoItem Folder Creation")]
    public bool SkipItemCreation { get; set; } = false;

    [DisplayName("Show System Message In Game")]
    public bool SystemLog { get; set; } = false;

    [DisplayName("Show Item Messages That Don't Involve You In Game, Uses The Following Flags Too")]
    public bool OtherLog { get; set; } = true;

    [DisplayName("Show Items That Are Progressive In Game")]
    public bool ProgressionLog { get; set; } = true;

    [DisplayName("Show Items That Are Useful In Game")]
    public bool UsefulLog { get; set; } = true;

    [DisplayName("Show Items That Are Filler In Game")]
    public bool FillerLog { get; set; } = true;

    [DisplayName("Show Items That Are Traps In Game")]
    public bool TrapLog { get; set; } = true;

}