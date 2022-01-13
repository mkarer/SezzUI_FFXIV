using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
    [Section("Cooldown HUD")]
    [SubSection("General", 0)]
    public class CooldownHudConfig : PluginConfigObject
    {
        public new static CooldownHudConfig DefaultConfig()
        {
            return new CooldownHudConfig()
            {
                Enabled = false,
            };
        }
    }
}
