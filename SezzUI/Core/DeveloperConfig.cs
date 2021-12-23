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
    }
}
