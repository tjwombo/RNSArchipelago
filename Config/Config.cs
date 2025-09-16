using System.ComponentModel;

namespace RnSArchipelago.Config;

public class Config : Configurable<Config>
{
    [DisplayName("Archiepelago Cache Location")]
    //[DefaultValue(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Archipelago\\Cache")]
    public string Cache { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Archipelago\\Cache";

    [DisplayName("Show System Message In Log")]
    [DefaultValue(false)]
    public bool SystemLog { get; set; } = false;

    [DisplayName("Show Item Messages That Don't Involve You In Log (Uses The Other Flags)")]
    [DefaultValue(true)]
    public bool OtherLog { get; set; } = true;

    [DisplayName("Show Items That Are Progressive In Log")]
    [DefaultValue(true)]
    public bool ProgressionLog { get; set; } = true;

    [DisplayName("Show Items That Are Useful In Log")]
    [DefaultValue(true)]
    public bool UsefulLog { get; set; } = true;

    [DisplayName("Show Items That Are Filler In Log")]
    [DefaultValue(true)]
    public bool FillerLog { get; set; } = true;

    [DisplayName("Show Items That Are Traps In Log")]
    [DefaultValue(true)]
    public bool TrapLog { get; set; } = true;

}