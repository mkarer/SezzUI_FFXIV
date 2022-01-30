using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
#if DEBUG
	[Disableable(false)]
	[Exportable(false)]
	[Section("Game UI")]
	[SubSection("Action Bars DEBUG", 0)]
	public class ActionBarDebugConfig : PluginConfigObject
	{
		[Checkbox("Log Bar Paging")]
		[Order(3)]
		public bool LogBarPaging = true;

		[Checkbox("Log Configuration Manager")]
		[Order(5)]
		public bool LogConfigurationManager = false;

		[Checkbox("Log General Messages")]
		[Order(1)]
		public bool LogGeneral = true;

		[Checkbox("Log Layout")]
		[Order(2)]
		public bool LogLayout = true;

		[Checkbox("Log RawInput Event Handling")]
		[Order(10)]
		public bool LogRawInputEventHandling = false;

		[Checkbox("Log SigScanner")]
		[Order(2)]
		public bool LogSigScanner = false;

		public new static ActionBarDebugConfig DefaultConfig() => new();
	}
#endif
}