using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[DisableParentSettings("Size")]
	[Section("Job HUD")]
	[SubSection("General", 0)]
	public class JobHudConfig : MovablePluginConfigObject
	{
		public new static JobHudConfig DefaultConfig() =>
			new()
			{
				Enabled = true,
				Position = new(0, 150)
			};
	}
}