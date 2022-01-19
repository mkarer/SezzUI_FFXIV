using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[Section("Cooldown HUD")]
	[SubSection("General", 0)]
	public class CooldownHudConfig : PluginConfigObject
	{
		public new static CooldownHudConfig DefaultConfig() => new() {Enabled = false,};

		[NestedConfig("Pulse Animation", 20)]
		public CooldownHudPulseConfig CooldownHudPulse = new();
	}

	public class CooldownHudPulseConfig : AnchorablePluginConfigObject
	{
		[DragInt("Animation Delay [ms]", min = -5000, max = 0)]
		[Order(30, collapseWith = nameof(Enabled))]
		public int Delay = -400;

		public new static CooldownHudPulseConfig DefaultConfig() =>
			new()
			{
				Enabled = false,
				Position = new(430f, -208f),
				Delay = 0,
				Size = new(32f, 32f),
				Anchor = Enums.DrawAnchor.Center
			};
	}
}