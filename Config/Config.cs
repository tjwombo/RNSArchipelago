using System.ComponentModel;

namespace RnSArchipelago.Config;

public class Config : Configurable<Config>
{
    public enum ItemType
    {
        All = 0x111,
        Progressive_Usefull = 0x011,
        Progressive = 0x001,
        None = 0x000
    }

    [DisplayName("Starting Configs")]
    public StartUpConfig StartUpConfig { get; set; } = new StartUpConfig();

    [DisplayName("Hint Configs")]
    public HintConfig HintConfig { get; set; } = new HintConfig();

    [DisplayName("Archipelago In Game Messages Configs")]
    public MessageConfig MessageConfig { get; set; } = new MessageConfig();

}