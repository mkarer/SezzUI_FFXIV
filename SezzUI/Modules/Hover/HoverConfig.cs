using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Enums;
using System.Numerics;

namespace SezzUI.Interface.GeneralElements
{
    [Section("Hover")]
    [SubSection("General", 0)]
    public class HoverConfig : PluginConfigObject
    {
        public new static HoverConfig DefaultConfig()
        {
            return new HoverConfig()
            {
                Enabled = true,
            };
        }
    }
}
