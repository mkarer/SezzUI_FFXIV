using System;
using ImGuiNET;
using System.Numerics;

namespace SezzUI.Helpers
{
	public static class DrawHelper
	{
        public static void DrawBackdrop(Vector2 pos, Vector2 size, uint backgroundColor, uint borderColor, ImDrawListPtr drawList)
		{
            // Background
            drawList.AddRectFilled(pos, pos + size, backgroundColor, 0);

            // Border
            drawList.AddRect(pos, pos + size, borderColor, 0, ImDrawFlags.None, 1);
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
            DrawBackdrop(pos, size, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * opacity)), drawList);

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

        public static void DrawProgressSwipe(Vector2 pos, Vector2 size, float remaining, float total, float opacity, ImDrawListPtr drawList)
        {
            // TODO: HUD Clipping
            if (total > 0)
            {
                float percent = 1 - (total - remaining) / total;

                float radius = (float)Math.Sqrt(Math.Pow(Math.Max(size.X, size.Y), 2) * 2) / 2f;
                float startAngle = -(float)Math.PI / 2;
                float endAngle = startAngle - 2f * (float)Math.PI * percent;

                ImGui.PushClipRect(pos, pos + size, false);
                drawList.PathArcTo(pos + size / 2, radius / 2, startAngle, endAngle, (int)(100f * Math.Abs(percent)));
                uint progressAlpha = (uint)(0.6f * 255 * opacity) << 24;
                drawList.PathStroke(progressAlpha, ImDrawFlags.None, radius);
                if (remaining != 0)
                {
                    Vector2 vec = new Vector2((float)Math.Cos(endAngle), (float)Math.Sin(endAngle));
                    Vector2 start = pos + size / 2;
                    Vector2 end = start + vec * radius;
                    Vector4 swipeLineColor = new Vector4(1, 1, 1, 0.3f * opacity);
                    uint color = ImGui.ColorConvertFloat4ToU32(swipeLineColor);

                    drawList.AddLine(start, end, color, 1);
                    drawList.AddLine(start, new(pos.X + size.X / 2, pos.Y), color, 1);
                    drawList.AddCircleFilled(start + new Vector2(1 / 4, 1 / 4), 1 / 2, color);
                }

                ImGui.PopClipRect();
            }
        }

        public static void DrawCooldownText(Vector2 pos, Vector2 size, float cooldown, ImDrawListPtr drawList, string font = "MyriadProLightCond_20", float opacity = 1)
		{
            // https://stackoverflow.com/questions/463642/what-is-the-best-way-to-convert-seconds-into-hourminutessecondsmilliseconds
            int cooldownRounded = (int)Math.Ceiling(cooldown);
            int seconds = cooldownRounded % 60;
            if (cooldownRounded >= 60)
            {
                int minutes = (cooldownRounded % 3600) / 60;
                DrawCenteredOutlineText(font, String.Format("{0:D1}:{1:D2}", minutes, seconds), pos, size, ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, opacity)), drawList);
            }
            else if (cooldown > 3)
            {
                DrawCenteredOutlineText(font, String.Format("{0:D1}", seconds), pos, size, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, opacity)), drawList);
            }
            else
            {
                DrawCenteredOutlineText(font, cooldown.ToString("0.0", Plugin.NumberFormatInfo), pos, size, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, opacity)), drawList);
            }
        }

        public static (Vector2, Vector2) GetTexCoordinates(Vector2 size, float clipOffset = 0f, bool isStatus = false)
        {
            float uv0x = isStatus ? 4f : 1f;
            float uv0y = isStatus ? 14f : 1f;

            float uv1x = isStatus ? 4f : 1f;
            float uv1y = isStatus ? 9f : 1f;

            Vector2 uv0 = new(uv0x / size.X, uv0y / size.Y);
            Vector2 uv1 = new(1f - uv1x / size.X, 1f - uv1y / size.Y);

            if (size.X != size.Y)
            {
                float ratio = Math.Max(size.X, size.Y) / Math.Min(size.X, size.Y);
                float crop = (1 - (1 / ratio)) / 2;

                if (size.X < size.Y)
                {
                    // Crop left/right parts
                    uv0.X += crop * (1 + clipOffset);
                    uv1.X -= crop * (1 - clipOffset);
                }
                else
                {
                    // Crop top/bottom parts
                    uv0.Y += crop * (1 + clipOffset);
                    uv1.Y -= crop * (1 - clipOffset);
                }
            }

            return (uv0, uv1);
        }

    }
}
