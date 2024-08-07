﻿using System;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;
using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Interface.BarManager;

public class BarManagerBar : IDisposable
{
	public BarManagerBarConfig Config;
	private readonly BarManager _parent;

	public uint Id = 0;

	public string? Text;
	public string? CountText;
	public uint? IconId;
	public object? Data;

	public long StartTime = 0;
	public uint Duration = 0;

	public uint Elapsed => IsActive ? Duration - Remaining : 0;
	public uint Remaining => IsActive ? Duration - (uint) (Environment.TickCount64 - StartTime) : 0;
	public bool IsActive => Duration > 0 && StartTime + Duration > Environment.TickCount64;

	public BarManagerBar(BarManager parent)
	{
		_parent = parent;
		Config = parent.BarConfig;
	}

	public void Draw(Vector2 position)
	{
		string windowId = $"SezzUI_BarManager_{_parent.Id}_{Id}";
		DrawHelper.DrawInWindow(windowId, position, Config.Size, false, false, drawList =>
		{
			switch (Config.Style)
			{
				case BarManagerStyle.Ruri:
				{
					Vector2 posBar = position;
					Vector2 sizeBar = Config.Size;
					sizeBar.Y = (int) Math.Ceiling(sizeBar.Y / 3);
					posBar.Y += Config.Size.Y - sizeBar.Y;

					// Icon
					if (IconId != null)
					{
						IDalamudTextureWrap? icon = Singletons.Get<MediaManager>().GetTextureFromIconIdOverridable((uint) IconId, out _);
						if (icon != null && icon.ImGuiHandle != IntPtr.Zero)
						{
							Vector2 posIcon = new(position.X + Config.BorderSize, position.Y + Config.BorderSize);
							Vector2 sizeIcon = new(Config.Size.Y - 2 * Config.BorderSize, Config.Size.Y - 2 * Config.BorderSize);
							(Vector2 uv0, Vector2 uv1) = DrawHelper.GetTexCoordinates(sizeIcon);

							drawList.AddRectFilled(posIcon, posIcon + sizeIcon, Config.BackgroundColor.Base, 0);
							drawList.AddImage(icon.ImGuiHandle, posIcon, posIcon + sizeIcon, uv0, uv1, ImGui.ColorConvertFloat4ToU32(Vector4.One));

							posBar.X += sizeIcon.X + 4;
							sizeBar.X -= sizeIcon.X + 4;

							// Border
							if (Config.BorderSize > 0)
							{
								drawList.AddRect(position, position + sizeIcon.AddXY(Config.BorderSize * 2), Config.BorderColor.Base, 0, ImDrawFlags.None, Config.BorderSize);
								posBar.X += Config.BorderSize * 2;
								sizeBar.X -= Config.BorderSize * 2;
							}
						}
					}

					// Bar Border
					if (Config.BorderSize > 0)
					{
						drawList.AddRect(posBar, posBar + sizeBar, Config.BorderColor.Base, 0, ImDrawFlags.None, Config.BorderSize);
						posBar.X += Config.BorderSize;
						posBar.Y += Config.BorderSize;
						sizeBar.X -= Config.BorderSize * 2;
						sizeBar.Y -= Config.BorderSize * 2;
					}

					// Bar BG
					drawList.AddRectFilled(posBar, posBar + sizeBar, Config.BackgroundColor.Base, 0);

					// Bar
					float fillPercent = (Config.FillInverted ? Remaining : (float) Elapsed) / Duration;
					Vector2 sizeBarFilled = new(Config.FillDirection.IsHorizontal() ? sizeBar.X * fillPercent : sizeBar.X, !Config.FillDirection.IsHorizontal() ? sizeBar.Y * fillPercent : sizeBar.Y);
					drawList.AddRectFilled(posBar, posBar + sizeBarFilled, Config.FillColor.Base, 0);

					// Text: Name
					if (Text != null)
					{
						DrawHelper.DrawAnchoredText(Config.NameTextStyle, DrawAnchor.Left, Text, new(posBar.X, position.Y), new(0, Config.Size.Y), Config.NameTextColor.Base, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList, 5);
					}

					// Text: Count
					if (CountText != null)
					{
						Vector2 posText2 = Text == null ? new(posBar.X, position.Y) : new(posBar.X + 5 + (Text == null ? Vector2.Zero : ImGui.CalcTextSize(Text)).X, position.Y);
						DrawHelper.DrawAnchoredText(Config.CountTextStyle, DrawAnchor.Left, CountText, posText2, new(0, Config.Size.Y), Config.CountTextColor.Base, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList, 5);
					}

					// Text: Duration
					if (Config.ShowDuration)
					{
						DrawHelper.DrawAnchoredText(Config.NameTextStyle, DrawAnchor.Right, DrawHelper.FormatDuration(Config.ShowDurationRemaining ? Remaining : Elapsed, (ushort) Config.MillisecondsThreshold, false), position, Config.Size, Config.NameTextColor.Base, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList, -5);
					}
				}
					break;

				case BarManagerStyle.Classic:
				{
					Vector2 posBar = position;
					Vector2 sizeBar = Config.Size;

					// Border
					if (Config.BorderSize > 0)
					{
						drawList.AddRect(position, position + Config.Size, Config.BorderColor.Base, 0, ImDrawFlags.None, Config.BorderSize);
						posBar.X += Config.BorderSize;
						posBar.Y += Config.BorderSize;
						sizeBar.X -= 2 * Config.BorderSize;
						sizeBar.Y -= 2 * Config.BorderSize;
					}

					// Icon
					if (IconId != null)
					{
						IDalamudTextureWrap? icon = Singletons.Get<MediaManager>().GetTextureFromIconIdOverridable((uint) IconId, out _);
						if (icon != null && icon.ImGuiHandle != IntPtr.Zero)
						{
							Vector2 posIcon = new(position.X + Config.BorderSize, position.Y + Config.BorderSize);
							Vector2 sizeIcon = new(Config.Size.Y - 2 * Config.BorderSize, Config.Size.Y - 2 * Config.BorderSize);
							(Vector2 uv0, Vector2 uv1) = DrawHelper.GetTexCoordinates(sizeIcon);

							drawList.AddRectFilled(posIcon, posIcon + sizeIcon, Config.BackgroundColor.Base, 0);
							drawList.AddImage(icon.ImGuiHandle, posIcon, posIcon + sizeIcon, uv0, uv1, ImGui.ColorConvertFloat4ToU32(Vector4.One));

							posBar.X += sizeIcon.X;
							sizeBar.X -= sizeIcon.X;

							// Border
							if (Config.BorderSize > 0)
							{
								Vector2 posIconBorder = new(posIcon.X + sizeIcon.X, posIcon.Y);
								drawList.AddLine(posIconBorder, posIconBorder.AddY(sizeIcon.Y), Config.BorderColor.Base, Config.BorderSize);
								posBar.X += Config.BorderSize;
								sizeBar.X -= Config.BorderSize;
							}
						}
					}

					// Bar BG
					drawList.AddRectFilled(posBar, posBar + sizeBar, Config.BackgroundColor.Base, 0);

					// Bar
					float fillPercent = (Config.FillInverted ? Remaining : (float) Elapsed) / Duration;
					Vector2 sizeBarFilled = new(Config.FillDirection.IsHorizontal() ? sizeBar.X * fillPercent : sizeBar.X, !Config.FillDirection.IsHorizontal() ? sizeBar.Y * fillPercent : sizeBar.Y);
					drawList.AddRectFilled(posBar, posBar + sizeBarFilled, Config.FillColor.Base, 0);

					// Text: Name
					if (Text != null)
					{
						DrawHelper.DrawAnchoredText(Config.NameTextStyle, DrawAnchor.Left, Text, posBar, new(0, sizeBar.Y), Config.NameTextColor.Base, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList, 4);
					}

					// Text: Count
					if (CountText != null)
					{
						Vector2 posText2 = Text == null ? new(posBar.X, posBar.Y) : new(posBar.X + 5 + (Text == null ? Vector2.Zero : ImGui.CalcTextSize(Text)).X, posBar.Y);
						DrawHelper.DrawAnchoredText(Config.CountTextStyle, DrawAnchor.Left, CountText, posText2, new(0, Config.Size.Y), Config.CountTextColor.Base, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList, 5);
					}

					// Text: Duration
					if (Config.ShowDuration)
					{
						DrawHelper.DrawAnchoredText(Config.NameTextStyle, DrawAnchor.Right, DrawHelper.FormatDuration(Config.ShowDurationRemaining ? Remaining : Elapsed, (ushort) Config.MillisecondsThreshold, false), posBar, sizeBar, Config.NameTextColor.Base, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList, -4);
					}
				}
					break;

				default:
					DrawHelper.DrawPlaceholder(Text ?? string.Empty, position, Config.Size, 1, PlaceholderStyle.Diagonal, drawList);
					break;
			}
		});
	}

	#region Destructor

	~BarManagerBar()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected void Dispose(bool disposing)
	{
		if (!disposing)
		{
		}
	}

	#endregion
}