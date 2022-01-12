using SezzUI.Config;
using SezzUI.Config.Attributes;
using ImGuiNET;
using System.Numerics;
using Newtonsoft.Json;

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
        [JsonIgnore] private static Vector4 titleColor = new(0f / 255f, 174f / 255f, 255f / 255f, 1f);

        [ManualDraw]
        public bool Draw(ref bool changed)
        {
            ImGui.TextColored(titleColor, "DelvUI");
            ImGui.Text("License: GNU AGPL v3");
            ImGui.Text("Website:");
            ImGui.SameLine();
            if (ImGui.Button("https://github.com/DelvUI/DelvUI", new Vector2(0, 0)))
            {
                DelvUI.Helpers.Utils.OpenUrl("https://github.com/DelvUI/DelvUI");
            }
            ImGui.Text("Most of the base framework and configuration code is taken from DelvUI which saved me a tremendous amount of time. Big shoutout!");
            ImGui.NewLine();

            ImGui.TextColored(titleColor, "Additional credits:");
            ImGui.Text("CaiClone, Lichie");

            return false;
        }
    }
}
