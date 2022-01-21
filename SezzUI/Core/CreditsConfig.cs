using System.Numerics;
using DelvUI.Helpers;
using ImGuiNET;
using Newtonsoft.Json;
using SezzUI.Config;
using SezzUI.Config.Attributes;

namespace SezzUI.Interface.GeneralElements
{
	[Disableable(false)]
	[Exportable(false)]
	[Shareable(false)]
	[Resettable(false)]
	[Section("Credits")]
	[SubSection("Credits", 0)]
	public class CreditsConfig : PluginConfigObject
	{
		public new static CreditsConfig DefaultConfig() => new();

		[JsonIgnore]
		private static readonly Vector4 _titleColor = new(0f / 255f, 174f / 255f, 255f / 255f, 1f);

		[ManualDraw]
		public bool Draw(ref bool changed)
		{
			ImGui.TextColored(_titleColor, "DelvUI");
			ImGui.Text("License: GNU AGPL v3");
			ImGui.Text("Website:");
			ImGui.SameLine();
			if (ImGui.Button("https://github.com/DelvUI/DelvUI", new(0, 0)))
			{
				Utils.OpenUrl("https://github.com/DelvUI/DelvUI");
			}

			ImGui.Text("Most of the base framework and configuration code is taken from DelvUI which saved me a tremendous amount of time. Big shoutout!");
			ImGui.NewLine();

			ImGui.TextColored(_titleColor, "Additional credits:");
			ImGui.Text("CaiClone, Lichie, professorK");

			return false;
		}
	}
}