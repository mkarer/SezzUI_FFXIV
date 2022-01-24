using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
#if DEBUG
	[Disableable(false)]
	[Section("Plugin Menu")]
	[SubSection("DEBUG", 0)]
	[Exportable(false)]
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