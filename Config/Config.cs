using System.ComponentModel;

namespace RnSArchipelago.Config;

public class Config : Configurable<Config>
{
    [DisplayName("Reloaded-II Mods Location")]
    //[DefaultValue(new DirectoryInfo(Environment.ExpandEnvironmentVariables("%RELOADEDIIMODS%")).FullName]
    public string Mods { get; set; } = new DirectoryInfo(Environment.ExpandEnvironmentVariables("%RELOADEDIIMODS%")).FullName;

    [DisplayName("Skip Item Creation")]
    [DefaultValue(false)]
    public bool SkipItemCreation { get; set; } = false;

    [DisplayName("Show System Message In Game")]
    [DefaultValue(false)]
    public bool SystemLog { get; set; } = false;

    [DisplayName("Show Item Messages That Don't Involve You In Game, Uses The Following Flags Too")]
    [DefaultValue(true)]
    public bool OtherLog { get; set; } = true;

    [DisplayName("Show Items That Are Progressive In Game")]
    [DefaultValue(true)]
    public bool ProgressionLog { get; set; } = true;

    [DisplayName("Show Items That Are Useful In Game")]
    [DefaultValue(true)]
    public bool UsefulLog { get; set; } = true;

    [DisplayName("Show Items That Are Filler In Game")]
    [DefaultValue(true)]
    public bool FillerLog { get; set; } = true;

    [DisplayName("Show Items That Are Traps In Game")]
    [DefaultValue(true)]
    public bool TrapLog { get; set; } = true;

}