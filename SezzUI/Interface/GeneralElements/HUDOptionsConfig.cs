using System;
using System.Numerics;
using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[Disableable(false)]
	[Section("Misc")]
	[SubSection("HUD Options", 0)]
	public class HUDOptionsConfig : PluginConfigObject
	{
		[Checkbox("Dim SezzUI's settings window when not focused")]
		[Order(10)]
		public bool DimConfigWindow = false;

		[Checkbox("Automatically disable HUD elements preview", help = "Enabling this will make it so all HUD elements preview modes are disabled when SezzUI's setting window is closed.")]
		[Order(35)]
		public bool AutomaticPreviewDisabling = true;

		[Checkbox("Enable Game Windows Clipping", separator = true, isMonitored = true, help = "Disabling this will make it so SezzUI elements are not covered by in-game elements.\nMight help with performance issues and/or random crashes.")]
		[Order(300)]
		public bool EnableClipRects = true;

		[Checkbox("Hide Elements Instead of Clipping", isMonitored = true, help = "This will make it so at least the SezzUI elements are completely hidden any in-game element is on top of them.\nIt will prevent SezzUI elements from covering in-game elements, but it won't look as good as clipping.\nMight help with performance issues and/or random crashes.")]
		[Order(301, collapseWith = nameof(EnableClipRects))]
		public bool HideInsteadOfClip = false;

		public new static HUDOptionsConfig DefaultConfig() => new();
	}

	public class HUDOptionsConfigConverter : PluginConfigObjectConverter
	{
		public HUDOptionsConfigConverter()
		{
			Func<Vector2, Vector2[]> func = value =>
			{
				Vector2[] array = new Vector2[4];
				for (int i = 0; i < 4; i++)
				{
					array[i] = value;
				}

				return array;
			};
		}

		public override bool CanConvert(Type objectType) => objectType == typeof(HUDOptionsConfig);
	}
}