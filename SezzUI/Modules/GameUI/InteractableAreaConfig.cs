using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Enums;
using System.Numerics;

namespace SezzUI.Interface.GeneralElements
{
    public class InteractableAreaConfig : AnchorablePluginConfigObject
    {
        public new static InteractableAreaConfig DefaultConfig()
        {
            return new InteractableAreaConfig()
            {
                Enabled = true,
            };
        }
    }
}
