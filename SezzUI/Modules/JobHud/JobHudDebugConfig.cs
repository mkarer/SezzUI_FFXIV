using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Modules.JobHud
{
#if DEBUG
	[Disableable(false)]
	[Exportable(false)]
	[Section("Job HUD")]
	[SubSection("DEBUG", 0)]
	public class JobHudDebugConfig : PluginConfigObject
	{
		[Checkbox("Log General Messages")]
		[Order(1)]
		public bool LogGeneral = true;

		[Checkbox("Log Configuration Manager")]
		[Order(5)]
		public bool LogConfigurationManager = false;

		public new static JobHudDebugConfig DefaultConfig() => new();
	}
#endif
}