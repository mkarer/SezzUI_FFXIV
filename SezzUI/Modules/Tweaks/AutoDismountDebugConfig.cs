using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Modules.Tweaks;
#if DEBUG
[Disableable(false)]
[Exportable(false)]
[Section("Tweaks")]
[SubSection("DEBUG", 0)]
public class AutoDismountDebugConfig : PluginConfigObject
{
	[Checkbox("Log General Messages")]
	[Order(1)]
	public bool LogGeneral = true;

	[Checkbox("Log Configuration Manager")]
	[Order(5)]
	public bool LogConfigurationManager = false;

	[Checkbox("Log Queued Actions")]
	[Order(10)]
	public bool LogQueuedActions = true;

	[Checkbox("Log ALL Used Actions (UseAction Hook)")]
	[Order(15)]
	public bool LogAllUsedActions = true;

	public new static AutoDismountDebugConfig DefaultConfig() => new();
}
#endif