using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
    [Section("Cooldown HUD")]
    [SubSection("General", 0)]
    public class CooldownHudConfig : PluginConfigObject
    {
        [NestedConfig("Pulse Animation", 35)]
        public CooldownHudPulseConfig CooldownHudPulse = new CooldownHudPulseConfig();

        public new static CooldownHudConfig DefaultConfig()
        {
            return new CooldownHudConfig()
            {
                Enabled = false,
            };
        }
    }

    [Exportable(false)]
    public class CooldownHudPulseConfig : AnchorablePluginConfigObject
    {
        [DragInt("Animation Delay [ms]", min = -5000, max = 0)]
        [Order(10, collapseWith = nameof(Enabled))]
        public int Delay = -400;

        public new static CooldownHudPulseConfig DefaultConfig() {
            return new CooldownHudPulseConfig()
            {
                Enabled = false,
                Position = new(430f, -208f),
                Delay = 0,
                Size = new(32f, 32f),
                Anchor = Enums.DrawAnchor.Center
            };
        }
    }
}
