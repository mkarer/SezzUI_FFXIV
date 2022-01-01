using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
    [Section("Game UI")]
    [SubSection("Element Hiding", 0)]
    public class ElementHiderConfig : PluginConfigObject
    {
        [Checkbox("Hide ActionBar Lock", isMonitored = true)]
        [Order(1)]
        public bool HideActionBarLock = false;

        public new static ElementHiderConfig DefaultConfig()
        {
            return new ElementHiderConfig()
            {
                Enabled = true,
            };
        }
    }
}
