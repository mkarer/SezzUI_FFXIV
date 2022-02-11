using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[Disableable(false)]
	[Section("General")]
	[SubSection("General", 0)]
	public class GeneralConfig : PluginConfigObject
	{
		public void Reset()
		{
			Enabled = true;
		}

		public GeneralConfig()
		{
			Reset();
		}

		public new static GeneralConfig DefaultConfig() => new();
	}
}