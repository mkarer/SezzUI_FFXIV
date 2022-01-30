using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Enums;

namespace SezzUI.Interface.GeneralElements
{
	[DisableParentSettings("Size", "Anchor")]
	[Section("Job HUD")]
	[SubSection("General", 0)]
	public class JobHudConfig : AnchorablePluginConfigObject
	{
		public void Reset()
		{
			Enabled = true;
			Position = new(0, 150);
			Anchor = DrawAnchor.Center;
		}

		public JobHudConfig()
		{
			Reset();
		}

		public new static JobHudConfig DefaultConfig() => new();
	}
}