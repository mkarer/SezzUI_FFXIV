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
		public bool HideActionBarLock;

		public ElementHiderConfig Reset()
		{
			Enabled = true;
			HideActionBarLock = true;
			return this;
		}

		public new static ElementHiderConfig DefaultConfig() => new ElementHiderConfig().Reset();
	}
}