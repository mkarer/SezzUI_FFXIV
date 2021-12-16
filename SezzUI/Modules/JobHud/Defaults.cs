using System.Numerics;
using System.Collections.Generic;

namespace SezzUI.Modules.JobHud
{
	public class IconColor
	{
		public Vector4 Icon;
		public Vector4 Border;
		public Vector4 Gloss;
	}

	public enum IconState
	{
		FadedOut,
		Soon,
		Warning,
		WarningOOM
	}

	public static class Defaults
	{
		public static readonly Dictionary<IconState, IconColor> StateColors = new()
		{
			{ IconState.FadedOut, new IconColor { Icon = new Vector4(1, 1, 1, 0.4f), Border = new Vector4(1, 1, 1, 0.3f), Gloss = new Vector4(1, 1, 1, 0.25f) } },
			{ IconState.Soon, new IconColor { Icon = new Vector4(0.7f, 0.9f, 1, 0.8f), Border = new Vector4(1, 0, 0, 0.6f), Gloss = new Vector4(1, 1, 1, 0.25f) } },
			{ IconState.Warning, new IconColor { Icon = new Vector4(1, 1, 1, 0.9f), Border = new Vector4(1, 0, 0, 0.6f), Gloss = new Vector4(1, 1, 1, 0.25f) } },
			{ IconState.WarningOOM, new IconColor { Icon = new Vector4(0.7f, 0.7f, 1, 0.9f), Border = new Vector4(1, 0, 0, 0.6f), Gloss = new Vector4(1, 1, 1, 0.25f) } }
		};

		// Icon Status Progress Bar
		public static readonly Vector4 IconBarColor = new(0.24f, 0.78f, 0.92f, 1);
		public static readonly Vector4 IconBarBGColor = new(0.1f, 0.1f, 0.1f, 0.8f);
		public static readonly Vector4 IconBarSeparatorColor = new(0.2f, 0.2f, 0.2f, 1);
	}
}
