using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using System;
using System.Numerics;

namespace SezzUI.Helpers
{
    public enum TextStyle
	{
        Normal,
        Shadowed,
        Outline
	}

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

        public static void DrawCenteredText(string font, TextStyle style, string text, Vector2 pos, Vector2 size, uint color, uint effectColor, ImDrawListPtr drawList)
		{
            bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont(font);

            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(pos + size / 2, textSize, DelvUI.Enums.DrawAnchor.Center);
            textPosition.Y += 1;

            switch (style)
			{
                case TextStyle.Normal:
                    drawList.AddText(textPosition, color, text);
                    break;

                case TextStyle.Shadowed:
                    DelvUI.Helpers.DrawHelper.DrawShadowText(text, textPosition, color, effectColor, drawList, 1);
                    break;

                case TextStyle.Outline:
                    DelvUI.Helpers.DrawHelper.DrawOutlinedText(text, textPosition, color, effectColor, drawList, 1);
                    break;
            }

            if (fontPushed) { ImGui.PopFont(); }
        }

        public static void DrawCenteredShadowText(string font, string text, Vector2 pos, Vector2 size, uint color, uint shadowColor, ImDrawListPtr drawList)
        {
            DrawCenteredText(font, TextStyle.Shadowed, text, pos, size, color, shadowColor, drawList);
        }

        public static void DrawCenteredOutlineText(string font, string text, Vector2 pos, Vector2 size, uint color, uint outlineColor, ImDrawListPtr drawList)
        {
            DrawCenteredText(font, TextStyle.Outline, text, pos, size, color, outlineColor, drawList);
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
            DrawCenteredShadowText("MyriadProLightCond_16", text, pos, size, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, opacity)), drawList);
        }
    }
}
