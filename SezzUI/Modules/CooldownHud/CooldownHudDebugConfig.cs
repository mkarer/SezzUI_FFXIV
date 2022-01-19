using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Modules.CooldownHud
{
#if DEBUG
	[Disableable(false)]
	[Section("Cooldown HUD")]
	[SubSection("DEBUG", 0)]
	[Exportable(false)]
	public class CooldownHudDebugConfig : PluginConfigObject
	{
		public new static CooldownHudDebugConfig DefaultConfig() => new();

		[Checkbox("Log General Messages")]
		[Order(1)]
		public bool LogGeneral = false;

		[Checkbox("Log Configuration Manager")]
		[Order(5)]
		public bool LogConfigurationManager = false;

		[Checkbox("Log Cooldown (Un-)Registration")]
		[Order(10)]
		public bool LogCooldownRegistration = false;

		[Checkbox("Log Cooldown Event Handling")]
		[Order(10)]
		public bool LogCooldownEventHandling = false;

		[Checkbox("Log Cooldown Pulse Animations")]
		[Order(10)]
		public bool LogCooldownPulseAnimations = false;
	}
#endif
}