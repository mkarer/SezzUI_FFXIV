using SezzUI.Config;

namespace SezzUI.Interface.GeneralElements
{
	public class InteractableAreaConfig : AnchorablePluginConfigObject
	{
		public new static InteractableAreaConfig DefaultConfig() =>
			new()
			{
				Enabled = true
			};
	}
}