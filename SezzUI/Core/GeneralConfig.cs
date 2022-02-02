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

		[SelectFolder("Custom Media Path", isMonitored = true, help = "Subfolder Structure:\nFonts\\*.ttf => Custom Fonts\nIcons\\IconPath => Status/Action Icon Override\n    Example: \"/i/013000/013403.png\" would go in MediaPath\\Icons\\013000\\013403.png\nIcons\\*.png => PluginMenu Icons\nImages\\Overlays\\*.png => Custom Aura Alert Overlays")]
		[Order(10)]
		public string CustomMediaPath = "";
	}
}