using Newtonsoft.Json;
using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[Disableable(false)]
	[Section("General")]
	[SubSection("General", 0)]
	public class GeneralConfig : PluginConfigObject
	{
		[NestedConfig("Custom Media", 20, spacing = false)]
		public GeneralMediaConfig Media = new();

		public void Reset()
		{
			Enabled = true;
			Media.Reset();
		}

		public GeneralConfig()
		{
			Reset();
		}

		public new static GeneralConfig DefaultConfig() => new();
	}

	[Disableable(false)]
	public class GeneralMediaConfig : PluginConfigObject
	{
		[SelectFolder("Path", isMonitored = true)]
		[Order(10)]
		public string Path = "";

		[Text("PathHelp", "Subfolder Structure:\nFonts\\*.ttf    >    Fonts\nIcons\\IconPath    >    Status/Action Icon Override\n     Example: IconPath \"/i/013000/013403.png\" would get looked up in MediaPath\\Icons\\013000\\013403.png\n     Additional note: Don't add _hr1 to your icons!\nIcons\\*.png    >    PluginMenu Icons\nImages\\Overlays\\*.png    >    Aura Alert Overlays")]
		[Order(15)]
		[JsonIgnore]
		public string PathHelp = null!;

		public void Reset()
		{
			Enabled = true;
			Path = "";
		}

		public GeneralMediaConfig()
		{
			Reset();
		}

		public new static GeneralMediaConfig DefaultConfig() => new();
	}
}