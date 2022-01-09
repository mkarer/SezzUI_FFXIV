using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
    [Disableable(false)]
    [Section("General")]
    [SubSection("Developer", 0)]
    public class DeveloperConfig : PluginConfigObject
    {
        public new static DeveloperConfig DefaultConfig() => new();
  
        [Checkbox("Show Banner", isMonitored = true)]
        [Order(1)]
        public bool ShowSezzUIButton = false;

        [Checkbox("Enable Animation", isMonitored = true)]
        [Order(2, collapseWith = nameof(ShowSezzUIButton))]
        public bool AnimateSezzUIButton = true;

        [Checkbox("Show Configuration on Login")]
        [Order(10)]
        public bool ShowConfigurationOnLogin = false;

        // Event Logging
        [Checkbox("Enable Event Logging [LogLevel: Debug]", spacing = true)]
        [Order(100)]
        public bool LogEvents = false;

        [Checkbox("Game: AddonsLoaded")]
        [Order(110, collapseWith = nameof(LogEvents))]
        public bool LogEventGameAddonsLoaded = false;

        [Checkbox("Game: AddonsVisibilityChanged")]
        [Order(111, collapseWith = nameof(LogEvents))]
        public bool LogEventGameAddonsVisibilityChanged = false;

        [Checkbox("Game: HudLayoutActivated")]
        [Order(112, collapseWith = nameof(LogEvents))]
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
    }
}
