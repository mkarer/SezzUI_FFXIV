using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[DisableParentSettings("Size")]
	[Section("Job HUD")]
	[SubSection("General", 0)]
	public class JobHudConfig : MovablePluginConfigObject
	{
		public JobHudConfig Reset()
		{
			Enabled = true;
			Position = new(0, 150);
			return this;
		}

		public new static JobHudConfig DefaultConfig() => new JobHudConfig().Reset();
	}
}