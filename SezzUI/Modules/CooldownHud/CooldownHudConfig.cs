using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Enums;

namespace SezzUI.Interface.GeneralElements
{
	[Section("Cooldown HUD")]
	[SubSection("General", 0)]
	public class CooldownHudConfig : PluginConfigObject
	{
		[NestedConfig("Pulse Animation", 20)]
		public CooldownHudPulseConfig CooldownHudPulse = new();

		public CooldownHudConfig Reset()
		{
			Enabled = true;
			CooldownHudPulse.Reset();
			return this;
		}

		public new static CooldownHudConfig DefaultConfig() => new CooldownHudConfig().Reset();
	}

	public class CooldownHudPulseConfig : AnchorablePluginConfigObject
	{
		[DragInt("Animation Delay [ms]", min = -5000, max = 0)]
		[Order(30, collapseWith = nameof(Enabled))]
		public int Delay = -400;

		public CooldownHudPulseConfig Reset()
		{
			Enabled = true;
			Delay = -400;
			Position = new(430f, -208f);
			Size = new(32f, 32f);
			Anchor = DrawAnchor.Center;
			return this;
		}

		public new static CooldownHudPulseConfig DefaultConfig() => new CooldownHudPulseConfig().Reset();
	}
}