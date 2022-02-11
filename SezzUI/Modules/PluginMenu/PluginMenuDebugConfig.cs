using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Modules.PluginMenu
{
#if DEBUG
	[Disableable(false)]
	[Exportable(false)]
	[Section("Plugin Menu")]
	[SubSection("DEBUG", 0)]
	public class PluginMenuDebugConfig : PluginConfigObject
	{
		[Checkbox("Log General Messages")]
		[Order(1)]
		public bool LogGeneral = true;

		[Checkbox("Log Configuration Manager")]
		[Order(5)]
		public bool LogConfigurationManager = false;

		public new static PluginMenuDebugConfig DefaultConfig() => new();
	}
#endif
}