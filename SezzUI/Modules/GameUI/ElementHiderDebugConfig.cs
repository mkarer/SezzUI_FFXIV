using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
#if DEBUG
	[Disableable(false)]
	[Section("Game UI")]
	[SubSection("Element Hiding DEBUG", 0)]
	[Exportable(false)]
	public class ElementHiderDebugConfig : PluginConfigObject
	{
		[Checkbox("Log General Messages")]
		[Order(1)]
		public bool LogGeneral = true;

		[Checkbox("Log Configuration Manager")]
		[Order(5)]
		public bool LogConfigurationManager = false;

		[Checkbox("Log Addons Event Handling")]
		[Order(10)]
		public bool LogAddonsEventHandling = false;

		[Checkbox("Log Visibility States")]
		[Order(15)]
		public bool LogVisibilityStates = false;

		[Checkbox("Log Visibility States (Verbose)")]
		[Order(15)]
		public bool LogVisibilityStatesVerbose = false;

		[Checkbox("Log Visibility Updates")]
		[Order(15)]
		public bool LogVisibilityUpdates = false;

		public new static ElementHiderDebugConfig DefaultConfig() => new();
	}
#endif
}