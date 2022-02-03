using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
#if DEBUG
	[Disableable(false)]
	[Exportable(false)]
	[Section("General")]
	[SubSection("DEBUG", 0)]
	public class GeneralDebugConfig : PluginConfigObject
	{
		public new static GeneralDebugConfig DefaultConfig() => new();

		[Checkbox("Show Configuration on Login")]
		[Order(10)]
		public bool ShowConfigurationOnLogin = false;

		// Component Logging
		[Checkbox("Enable Component Logging [LogLevel: Debug]", spacing = true)]
		[Order(200)]
		public bool LogComponents = false;

		[Checkbox("OriginalFunctionManager")]
		[Order(210, collapseWith = nameof(LogComponents))]
		public bool LogComponentsOriginalFunctionManager = false;

		[Checkbox("RawInputNativeWindow")]
		[Order(210, collapseWith = nameof(LogComponents))]
		public bool LogComponentsRawInputNativeWindow = false;

		[Checkbox("MediaManager")]
		[Order(210, collapseWith = nameof(LogComponents))]
		public bool LogComponentsMediaManager = false;

		[Checkbox("ImageCache")]
		[Order(210, collapseWith = nameof(LogComponents))]
		public bool LogComponentsImageCache = false;

		// Event Logging
		[Checkbox("Enable Event Logging [LogLevel: Debug]", spacing = true)]
		[Order(100)]
		public bool LogEvents = false;

		[Checkbox("Plugin: DrawStateChanged*")]
		[Order(110, collapseWith = nameof(LogEvents))]
		public bool LogEventPluginDrawStateChanged = false;

		[Checkbox("Game")]
		[Order(110, collapseWith = nameof(LogEvents))]
		public bool LogEventGame = false;

		[Checkbox("Game: AddonsLoaded")]
		[Order(110, collapseWith = nameof(LogEventGame))]
		public bool LogEventGameAddonsLoaded = false;

		[Checkbox("Game: AddonsVisibilityChanged")]
		[Order(111, collapseWith = nameof(LogEventGame))]
		public bool LogEventGameAddonsVisibilityChanged = false;

		[Checkbox("Game: HudLayoutActivated")]
		[Order(112, collapseWith = nameof(LogEventGame))]
		public bool LogEventGameHudLayoutActivated = false;

		[Checkbox("Player: JobChanged")]
		[Order(120, collapseWith = nameof(LogEvents))]
		public bool LogEventPlayerJobChanged = false;

		[Checkbox("Player: LevelChanged")]
		[Order(121, collapseWith = nameof(LogEvents))]
		public bool LogEventPlayerLevelChanged = false;

		[Checkbox("Combat: EnteringCombat")]
		[Order(130, collapseWith = nameof(LogEvents))]
		public bool LogEventCombatEnteringCombat = false;

		[Checkbox("Combat: LeavingCombat")]
		[Order(131, collapseWith = nameof(LogEvents))]
		public bool LogEventCombatLeavingCombat = false;

		[Checkbox("Cooldown: Hooks/EventHandler")]
		[Order(140, collapseWith = nameof(LogEvents))]
		public bool LogEventCooldownHooks = false;

		[Checkbox("Cooldown: Usage")]
		[Order(141, collapseWith = nameof(LogEvents))]
		public bool LogEventCooldownUsage = false;

		[Checkbox("Cooldown: CooldownStarted")]
		[Order(145, collapseWith = nameof(LogEvents))]
		public bool LogEventCooldownStarted = false;

		[Checkbox("Cooldown: CooldownChanged")]
		[Order(146, collapseWith = nameof(LogEvents))]
		public bool LogEventCooldownChanged = false;

		[Checkbox("Cooldown: CooldownFinished")]
		[Order(147, collapseWith = nameof(LogEvents))]
		public bool LogEventCooldownFinished = false;
	}
#endif
}