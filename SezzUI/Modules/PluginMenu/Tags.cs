using System.Text.RegularExpressions;

namespace SezzUI.Modules.PluginMenu
{
	public static class Tags
	{
		// World of Warcraft like text coloring: |cAARRGGBB
		public static Regex RegexColor = new("(?<color>\\|c[0-9a-f]{8}?)|(?<text>(?:(?!\\|c[0-9a-f]{8}?).)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		public static Regex RegexColorTags = new("(?<color>\\|c[0-9a-f]{8}?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	}
}