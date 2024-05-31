using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Modules.ServerInfoBar;
#if DEBUG
[Disableable(false)]
[Exportable(false)]
[Section("Server Info Bar")]
[SubSection("DEBUG", 0)]
public class ServerInfoBarDebugConfig : PluginConfigObject
{
	[Checkbox("Log General Messages")]
	[Order(1)]
	public bool LogGeneral = true;

	[Checkbox("Log Configuration Manager")]
	[Order(5)]
	public bool LogConfigurationManager = false;

	public new static ServerInfoBarDebugConfig DefaultConfig() => new();
}
#endif