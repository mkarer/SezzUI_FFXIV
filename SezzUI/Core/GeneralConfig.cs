using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[Disableable(false)]
	[Section("General")]
	[SubSection("General", 0)]
	public class GeneralConfig : PluginConfigObject
	{
		public new static GeneralConfig DefaultConfig() => new();

		[Checkbox("Enable Game UI Clipping", isMonitored = true, help = "Tries to hide parts of the overlay that would otherwise cover in-game elements.\nPlease note that this only works for the foremost element and also only handles the most common ones.\nDisabling this might help with performance issues and/or random crashes.")]
		[Order(300)]
		public bool EnableClipRects = true;

		[Checkbox("Hide overlay instead of clipping.", isMonitored = true, help = "This will hide overlay elements completely when any in-game element is on top of them.\nIt will prevent them from covering in-game elements, but it won't look as good as clipping.\nMight help with performance issues and/or random crashes.")]
		[Order(301, collapseWith = nameof(EnableClipRects))]
		public bool HideInsteadOfClip = false;
	}
}