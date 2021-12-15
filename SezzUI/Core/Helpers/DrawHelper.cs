using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using System;
using System.Numerics;

namespace SezzUI.Helpers
{
	public static class DrawHelper
	{
        public static void DrawBackdrop(Vector2 pos, Vector2 size, ImDrawListPtr drawList, float opacity = 1)
		{
            // Background
            drawList.AddRectFilled(pos, pos + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * opacity)), 0);

            // Border
            uint colorBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * opacity));
            drawList.AddRect(pos, pos + size, colorBorder, 0, ImDrawFlags.None, 1);
        }

        public static void DrawPlaceholder(string text, Vector2 pos, Vector2 size, ImDrawListPtr drawList, float opacity = 1)
		{
            // Backdrop
            DrawBackdrop(pos, size, drawList, opacity);

            // Cross
            uint colorLines = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.1f * opacity));
            drawList.AddLine(new Vector2(pos.X + 1, pos.Y + 1), new Vector2(pos.X + size.X - 1, pos.Y + size.Y - 2), colorLines, 1); // Top Left -> Bottom Right
            drawList.AddLine(new Vector2(pos.X + 1, pos.Y + size.Y - 2), new Vector2(pos.X + size.X - 1, pos.Y + 1), colorLines, 1); // Bottom Left -> Top Right

            // Text
            bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont("MyriadProLightCond_16");

            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(pos + size / 2, textSize, DelvUI.Enums.DrawAnchor.Center);
            textPosition.Y += 1;
            DelvUI.Helpers.DrawHelper.DrawShadowText(text, textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, opacity)), drawList, 1);

            if (fontPushed) { ImGui.PopFont(); }
        }
    }
}
