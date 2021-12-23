using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Enums;
using System.Numerics;

namespace SezzUI.Interface.GeneralElements
{
    [DisableParentSettings("Size")]
    [Section("Job Hud")]
    [SubSection("General", 0)]
    public class JobHudConfig : MovablePluginConfigObject
    {
        public new static JobHudConfig DefaultConfig()
        {
            return new JobHudConfig() {
                Enabled = true,
                Position = new Vector2(0, 150),
            };
        }
    }
}
