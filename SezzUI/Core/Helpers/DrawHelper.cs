using System;
using ImGuiNET;
using System.Numerics;

namespace SezzUI.Helpers
{
	public static class DrawHelper
	{
        public static void DrawBackdrop(Vector2 pos, Vector2 size, float opacity, ImDrawListPtr drawList)
		{
            // Background
            drawList.AddRectFilled(pos, pos + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * opacity)), 0);

            // Border
            uint colorBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * opacity));
            drawList.AddRect(pos, pos + size, colorBorder, 0, ImDrawFlags.None, 1);
        }

        public static void DrawAnchoredText(string font, Enums.TextStyle style, DelvUI.Enums.DrawAnchor anchor, string text, Vector2 pos, Vector2 size, uint color, uint effectColor, ImDrawListPtr drawList, float xOffset = 0, float yOffset = 0, string textPosCalc = "")
		{
            bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont(font);

            Vector2 textSize = ImGui.CalcTextSize(textPosCalc != "" ? textPosCalc : text);
            Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(anchor == DelvUI.Enums.DrawAnchor.Center ? pos + size / 2 : pos + size, textSize, anchor);
            textPosition.X += xOffset;
            textPosition.Y += 1 + yOffset;

            switch (style)
			{
                case Enums.TextStyle.Normal:
                    drawList.AddText(textPosition, color, text);
                    break;

                case Enums.TextStyle.Shadowed:
                    DelvUI.Helpers.DrawHelper.DrawShadowText(text, textPosition, color, effectColor, drawList, 1);
                    break;

                case Enums.TextStyle.Outline:
                    DelvUI.Helpers.DrawHelper.DrawOutlinedText(text, textPosition, color, effectColor, drawList, 1);
                    break;
            }

            if (fontPushed) { ImGui.PopFont(); }
        }

        public static void DrawCenteredShadowText(string font, string text, Vector2 pos, Vector2 size, uint color, uint shadowColor, ImDrawListPtr drawList)
        {
            DrawAnchoredText(font, Enums.TextStyle.Shadowed, DelvUI.Enums.DrawAnchor.Center, text, pos, size, color, shadowColor, drawList);
        }

        public static void DrawCenteredOutlineText(string font, string text, Vector2 pos, Vector2 size, uint color, uint outlineColor, ImDrawListPtr drawList)
        {
            DrawAnchoredText(font, Enums.TextStyle.Outline, DelvUI.Enums.DrawAnchor.Center, text, pos, size, color, outlineColor, drawList);
        }

        public static void DrawPlaceholder(string text, Vector2 pos, Vector2 size, float opacity, ImDrawListPtr drawList)
		{
            // Backdrop
            DrawBackdrop(pos, size, opacity, drawList);

            // Cross
            uint colorLines = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.1f * opacity));
            drawList.AddLine(new Vector2(pos.X + 1, pos.Y + 1), new Vector2(pos.X + size.X - 1, pos.Y + size.Y - 2), colorLines, 1); // Top Left -> Bottom Right
            drawList.AddLine(new Vector2(pos.X + 1, pos.Y + size.Y - 2), new Vector2(pos.X + size.X - 1, pos.Y + 1), colorLines, 1); // Bottom Left -> Top Right

            // Text
            DrawCenteredShadowText("MyriadProLightCond_16", text, pos, size, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, opacity)), drawList);
        }

        public static void DrawProgressBar(Vector2 pos, Vector2 size, float min, float max, float current, uint barColor, uint bgColor, ImDrawListPtr drawList)
		{
            drawList.AddRectFilled(pos, pos + size, bgColor, 0);

            float fillPercent = max == 0 ? 1f : Math.Clamp((current - min) / (max - min), 0f, 1f);
            drawList.AddRectFilled(pos, pos + new Vector2(size.X * fillPercent, size.Y), barColor, 0);
        }
    }
}
