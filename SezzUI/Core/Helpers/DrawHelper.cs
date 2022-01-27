using System;
using System.Numerics;
using DelvUI.Helpers;
using ImGuiNET;
using ImGuiScene;
using SezzUI.Enums;

namespace SezzUI.Helpers
{
	public static class DrawHelper
	{
		internal static PluginLogger Logger;

		static DrawHelper()
		{
			Logger = new("DrawHelper");
		}

		public static Vector2 GetAnchoredPosition(Vector2 parentPosition, Vector2 parentSize, Vector2 elementSize, DrawAnchor anchor)
		{
			return anchor switch
			{
				DrawAnchor.Center => parentPosition + parentSize / 2f - elementSize / 2f,
				DrawAnchor.Left => parentPosition + new Vector2(0, parentSize.Y / 2f - elementSize.Y / 2f),
				DrawAnchor.Right => new(parentPosition.X + parentSize.X - elementSize.X, parentPosition.Y + parentSize.Y / 2f - elementSize.Y / 2f),
				DrawAnchor.Top => parentPosition + new Vector2(parentSize.X / 2f - elementSize.X / 2f, 0),
				DrawAnchor.TopLeft => parentPosition,
				DrawAnchor.TopRight => parentPosition + new Vector2(parentSize.X - elementSize.X, 0),
				DrawAnchor.Bottom => parentPosition + new Vector2(parentSize.X / 2f - elementSize.X / 2f, parentSize.Y - elementSize.Y),
				DrawAnchor.BottomLeft => parentPosition + new Vector2(0, parentSize.Y - elementSize.Y),
				DrawAnchor.BottomRight => parentPosition + new Vector2(parentSize.X - elementSize.X, parentSize.Y - elementSize.Y),
				_ => parentPosition
			};
		}

		public static Vector2 GetAnchoredPosition(Vector2 elementSize, DrawAnchor anchor) => GetAnchoredPosition(Vector2.Zero, ImGui.GetMainViewport().Size, elementSize, anchor);

		public static void DrawBackdrop(Vector2 pos, Vector2 size, uint backgroundColor, uint borderColor, ImDrawListPtr drawList)
		{
			// Background
			drawList.AddRectFilled(pos, pos + size, backgroundColor, 0);

			// Border
			drawList.AddRect(pos, pos + size, borderColor, 0, ImDrawFlags.None, 1);
		}

		private static readonly Vector2[] _edgeUv0 =
		{
			new(0, 0), // Left
			new(1f / 8f, 0), // Right
			new(2f / 8f + 0.005f, 0), // Top
			new(3f / 8f + 0.005f, 0), // Bottom
			new(4f / 8f, 0), // Top Left
			new(5f / 8f, 0), // Top Right
			new(6f / 8f, 0), // Bottom Left
			new(7f / 8f, 0) // Bottom Right
		};

		private static readonly Vector2[] _edgeUv1 =
		{
			new(1f / 8f, 1), // Left
			new(2f / 8f, 1), // Left
			new(3f / 8f - 0.005f, 1), // Top
			new(4f / 8f - 0.005f, 1), // Bottom
			new(5f / 8f, 1), // Top Left
			new(6f / 8f, 1), // Top Right
			new(7f / 8f, 1), // Bottom Left
			new(1, 1) // Bottom Right
		};

		/// <summary>
		///     Draws an edgeFile from World of Warcraft around another element.
		///     See: https://wowpedia.fandom.com/wiki/EdgeFiles
		///     Please note: Currently you need to rotate the top and bottom (3rd and 4th segment) correctly until i figure out how
		///     to rotate textures using Dear ImGui.
		/// </summary>
		public static void DrawBackdropEdge(string edgeFile, Vector2 backdropPos, Vector2 backdropSize, uint glowColor, ImDrawListPtr drawList, uint size = 8, short inset = -8)
		{
			TextureWrap? texture = ImageCache.Instance.GetImageFromPath(edgeFile);
			if (texture == null)
			{
				return;
			}

			float leftX = backdropPos.X + inset;
			float rightX = backdropPos.X + backdropSize.X - inset - size;
			float horizontalX1 = backdropPos.X + (inset > 0 ? inset : inset + size);
			float horizontalX2 = backdropPos.X + backdropSize.X - (inset > 0 ? inset : inset + size);
			float topY = backdropPos.Y + inset;
			float bottomY = backdropPos.Y + backdropSize.Y - inset - size;
			float verticalY1 = backdropPos.Y + (inset > 0 ? inset : inset + size);
			float verticalY2 = backdropPos.Y + backdropSize.Y - (inset > 0 ? inset : inset + size);

			// Left
			drawList.AddImage(texture.ImGuiHandle, new(leftX, verticalY1), new(leftX + size, verticalY2), _edgeUv0[inset < 0 ? 0 : 1], _edgeUv1[inset < 0 ? 0 : 1], glowColor);
			// Right
			drawList.AddImage(texture.ImGuiHandle, new(rightX, verticalY1), new(rightX + size, verticalY2), _edgeUv0[inset < 0 ? 1 : 0], _edgeUv1[inset < 0 ? 1 : 0], glowColor);
			// Top
			drawList.AddImage(texture.ImGuiHandle, new(horizontalX1, topY), new(horizontalX2, topY + size), _edgeUv0[inset < 0 ? 2 : 3], _edgeUv1[inset < 0 ? 2 : 3], glowColor);
			// Bottom
			drawList.AddImage(texture.ImGuiHandle, new(horizontalX1, bottomY), new(horizontalX2, bottomY + size), _edgeUv0[inset < 0 ? 3 : 2], _edgeUv1[inset < 0 ? 3 : 2], glowColor);
			// Top Left
			drawList.AddImage(texture.ImGuiHandle, new(leftX, topY), new(leftX + size, topY + size), _edgeUv0[inset < 0 ? 4 : 7], _edgeUv1[inset < 0 ? 4 : 7], glowColor);
			// Top Right
			drawList.AddImage(texture.ImGuiHandle, new(rightX, topY), new(rightX + size, topY + size), _edgeUv0[inset < 0 ? 5 : 6], _edgeUv1[inset < 0 ? 5 : 6], glowColor);
			// Bottom Left
			drawList.AddImage(texture.ImGuiHandle, new(leftX, bottomY), new(leftX + size, bottomY + size), _edgeUv0[inset < 0 ? 6 : 5], _edgeUv1[inset < 0 ? 6 : 5], glowColor);
			// Bottom Right
			drawList.AddImage(texture.ImGuiHandle, new(rightX, bottomY), new(rightX + size, bottomY + size), _edgeUv0[inset < 0 ? 7 : 4], _edgeUv1[inset < 0 ? 7 : 4], glowColor);
		}

		public static void DrawBackdropEdgeGlow(Vector2 backdropPos, Vector2 backdropSize, uint glowColor, ImDrawListPtr drawList, uint size = 8, short inset = -8)
		{
			DrawBackdropEdge(Plugin.AssemblyLocation + "Media\\Images\\GlowTex.png", backdropPos, backdropSize, glowColor, drawList, size, inset);
		}

		public static void DrawAnchoredText(string font, TextStyle style, DrawAnchor anchor, string text, Vector2 pos, Vector2 size, uint color, uint effectColor, ImDrawListPtr drawList, float xOffset = 0, float yOffset = 0, string textPosCalc = "")
		{
			bool fontPushed = FontsManager.Instance.PushFont(font);

			Vector2 textSize = ImGui.CalcTextSize(textPosCalc != "" ? textPosCalc : text);
			Vector2 textPosition = GetAnchoredPosition(pos, size, textSize, anchor);
			textPosition.X += xOffset;
			textPosition.Y += 1 + yOffset;

			switch (style)
			{
				case TextStyle.Normal:
					drawList.AddText(textPosition, color, text);
					break;

				case TextStyle.Shadowed:
					DelvUI.Helpers.DrawHelper.DrawShadowText(text, textPosition, color, effectColor, drawList);
					break;

				case TextStyle.Outline:
					DelvUI.Helpers.DrawHelper.DrawOutlinedText(text, textPosition, color, effectColor, drawList);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(style), style, null);
			}

			if (fontPushed)
			{
				ImGui.PopFont();
			}
		}

		public static void DrawCenteredShadowText(string font, string text, Vector2 pos, Vector2 size, uint color, uint shadowColor, ImDrawListPtr drawList)
		{
			DrawAnchoredText(font, TextStyle.Shadowed, DrawAnchor.Center, text, pos, size, color, shadowColor, drawList);
		}

		public static void DrawCenteredOutlineText(string font, string text, Vector2 pos, Vector2 size, uint color, uint outlineColor, ImDrawListPtr drawList)
		{
			DrawAnchoredText(font, TextStyle.Outline, DrawAnchor.Center, text, pos, size, color, outlineColor, drawList);
		}

		public static void DrawPlaceholder(string text, Vector2 pos, Vector2 size, float opacity, ImDrawListPtr drawList)
		{
			// Backdrop
			DrawBackdrop(pos, size, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0.5f * opacity)), ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 0.3f * opacity)), drawList);

			// Cross
			uint colorLines = ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 0.1f * opacity));
			drawList.AddLine(new(pos.X + 1, pos.Y + 1), new(pos.X + size.X - 1, pos.Y + size.Y - 2), colorLines, 1); // Top Left -> Bottom Right
			drawList.AddLine(new(pos.X + 1, pos.Y + size.Y - 2), new(pos.X + size.X - 1, pos.Y + 1), colorLines, 1); // Bottom Left -> Top Right

			// Text
			DrawCenteredShadowText("MyriadProLightCond_16", text, pos, size, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, opacity)), drawList);
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
				if (remaining > total)
				{
					// avoid crashes
					Logger.Warning("DrawProgressSwipe", $"Adjusted remaining duration {remaining} to not exceed total {total}. FIX YOUR CODE!");
					remaining = total;
				}

				float percent = 1 - (total - remaining) / total;

				float radius = (float) Math.Sqrt(Math.Pow(Math.Max(size.X, size.Y), 2) * 2) / 2f;
				float startAngle = -(float) Math.PI / 2;
				float endAngle = startAngle - 2f * (float) Math.PI * percent;

				ImGui.PushClipRect(pos, pos + size, false);
				drawList.PathArcTo(pos + size / 2, radius / 2, startAngle, endAngle, (int) (100f * Math.Abs(percent)));
				uint progressAlpha = (uint) (0.6f * 255 * opacity) << 24;
				drawList.PathStroke(progressAlpha, ImDrawFlags.None, radius);
				if (remaining != 0)
				{
					Vector2 vec = new((float) Math.Cos(endAngle), (float) Math.Sin(endAngle));
					Vector2 start = pos + size / 2;
					Vector2 end = start + vec * radius;
					Vector4 swipeLineColor = new(1, 1, 1, 0.3f * opacity);
					uint color = ImGui.ColorConvertFloat4ToU32(swipeLineColor);

					drawList.AddLine(start, end, color, 1);
					drawList.AddLine(start, new(pos.X + size.X / 2, pos.Y), color, 1);
					drawList.AddCircleFilled(start + new Vector2(0.25f, 0.25f), 0.5f, color);
				}

				ImGui.PopClipRect();
			}
		}

		public static string FormatDuration(uint durationMs, ushort msThreshold = 0, bool zeroSeconds = true) => FormatDuration(durationMs / 1000f, msThreshold, zeroSeconds);

		public static string FormatDuration(float duration, ushort msThreshold = 0, bool zeroSeconds = true)
		{
			// TODO: Cleanup, quick and dirty Lua port...
			// https://stackoverflow.com/questions/463642/what-is-the-best-way-to-convert-seconds-into-hourminutessecondsmilliseconds
			if (duration >= 3600)
			{
				// 1 Hour+
				uint hours = (uint) Math.Floor(duration / 3600f);
				uint minutes = (uint) Math.Floor((duration - hours * 3600) / 60f);
				uint seconds = (uint) (duration - minutes * 60 - hours * 3600);
				return $"{hours:D1}:{minutes:D2}:{seconds:D2}";
			}

			if (duration >= 60)
			{
				// 1-59 Minutes
				uint minutes = (uint) Math.Floor(duration / 60f);
				uint seconds = (uint) (duration - minutes * 60);
				return $"{minutes:D1}:{seconds:D2}";
			}

			if (duration > msThreshold)
			{
				// Seconds
				return Math.Floor(duration).ToString(Plugin.NumberFormatInfo);
			}

			// Milliseconds
			return (Math.Truncate(duration * 10) / 10).ToString(zeroSeconds ? "0.0" : ".0", Plugin.NumberFormatInfo);
		}

		public static void DrawCooldownText(Vector2 pos, Vector2 size, float cooldown, ImDrawListPtr drawList, string font = "MyriadProLightCond_20", float opacity = 1)
		{
			ushort msThreshold = 3;

			if (cooldown >= 60)
			{
				DrawCenteredOutlineText(font, FormatDuration(cooldown), pos, size, ImGui.ColorConvertFloat4ToU32(new(0.6f, 0.6f, 0.6f, opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, opacity)), drawList);
			}
			else if (cooldown > msThreshold)
			{
				DrawCenteredOutlineText(font, FormatDuration(cooldown, msThreshold), pos, size, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, opacity)), drawList);
			}
			else
			{
				DrawCenteredOutlineText(font, FormatDuration(cooldown, msThreshold), pos, size, ImGui.ColorConvertFloat4ToU32(new(1, 0, 0, opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, opacity)), drawList);
			}
		}

		public static (Vector2, Vector2) GetTexCoordinates(Vector2 size, float clipOffset = 0f, bool isStatus = false)
		{
			float uv0X = isStatus ? 4f : 1f;
			float uv0Y = isStatus ? 14f : 1f;

			float uv1X = isStatus ? 4f : 1f;
			float uv1Y = isStatus ? 9f : 1f;

			Vector2 uv0 = new(uv0X / size.X, uv0Y / size.Y);
			Vector2 uv1 = new(1f - uv1X / size.X, 1f - uv1Y / size.Y);

			if (!size.X.Equals(size.Y))
			{
				float ratio = Math.Max(size.X, size.Y) / Math.Min(size.X, size.Y);
				float crop = (1 - 1 / ratio) / 2;

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