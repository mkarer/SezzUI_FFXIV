﻿using System;
using System.Numerics;
using SezzUI.Enums;
using ImGuiNET;
using ImGuiScene;

namespace SezzUI.BarManager
{
    public class BarManagerBar : IDisposable
    {
        public BarManagerBarConfig Config;
        private readonly BarManager _parent;

        public uint Id = 0;

        public string? Text;
        public TextureWrap? Icon;

        public long StartTime = 0;
        public uint Duration = 0;

        public uint Elapsed => IsActive ? Duration - Remaining : 0;
        public uint Remaining => IsActive ? Duration - (uint)(Environment.TickCount64 - StartTime) : 0;
        public bool IsActive => Duration > 0 && StartTime + Duration > Environment.TickCount64;

        public BarManagerBar(BarManager parent)
        {
            _parent = parent;
            Config = parent.BarConfig;
        }

        public void Draw(Vector2 position)
        {
            string windowId = $"SezzUI_BarManager_{_parent.Id}_{Id}";
            DelvUI.Helpers.DrawHelper.DrawInWindow(windowId, position, Config.Size, false, false, (drawList) =>
            {
                switch (Config.Style) {
                    case BarManagerStyle.Ruri:
                        { 
                            Vector2 posBar = position;
                            Vector2 sizeBar = Config.Size;
                            sizeBar.Y = (int)Math.Ceiling(sizeBar.Y / 3);
                            posBar.Y += Config.Size.Y - sizeBar.Y;

                            // Icon
                            if (Icon != null)
                            {
                                Vector2 posIcon = new(position.X + Config.BorderSize, position.Y + Config.BorderSize);
                                Vector2 sizeIcon = new(Config.Size.Y - 2 * Config.BorderSize, Config.Size.Y - 2 * Config.BorderSize);
                                (Vector2 uv0, Vector2 uv1) = Helpers.DrawHelper.GetTexCoordinates(sizeIcon);

                                drawList.AddRectFilled(posIcon, posIcon + sizeIcon, ImGui.ColorConvertFloat4ToU32(Config.BackgroundColor), 0);
                                drawList.AddImage(Icon.ImGuiHandle, posIcon, posIcon + sizeIcon, uv0, uv1, ImGui.ColorConvertFloat4ToU32(Vector4.One));

                                posBar.X += sizeIcon.X + 4;
                                sizeBar.X -= sizeIcon.X + 4;

                                // Border
                                if (Config.BorderSize > 0)
                                {
                                    drawList.AddRect(position, position + sizeIcon.AddXY(Config.BorderSize * 2), ImGui.ColorConvertFloat4ToU32(Config.BorderColor), 0, ImDrawFlags.None, Config.BorderSize);
                                    posBar.X += Config.BorderSize * 2;
                                    sizeBar.X -= Config.BorderSize * 2;
                                }
                            }

                            // Bar Border
                            if (Config.BorderSize > 0)
                            {
                                drawList.AddRect(posBar, posBar + sizeBar, ImGui.ColorConvertFloat4ToU32(Config.BorderColor), 0, ImDrawFlags.None, Config.BorderSize);
                                posBar.X += Config.BorderSize;
                                posBar.Y += Config.BorderSize;
                                sizeBar.X -= Config.BorderSize * 2;
                                sizeBar.Y -= Config.BorderSize * 2;
                            }

                            // Bar BG
                            drawList.AddRectFilled(posBar, posBar + sizeBar, ImGui.ColorConvertFloat4ToU32(Config.BackgroundColor), 0);

                            // Bar
                            float fillPercent = (Config.FillInverted ? (float)Remaining : (float)Elapsed) / (float)Duration;
                            Vector2 sizeBarFilled = new(Config.FillDirection.IsHorizontal() ? sizeBar.X * fillPercent : sizeBar.X, !Config.FillDirection.IsHorizontal() ? sizeBar.Y * fillPercent : sizeBar.Y);
                            drawList.AddRectFilled(posBar, posBar + sizeBarFilled, ImGui.ColorConvertFloat4ToU32(Config.FillColor), 0);

                            // Text: Name
                            if (Text != null)
                            {
                                Helpers.DrawHelper.DrawAnchoredText("MyriadProLightCond_18", Config.NameTextextStyle, DrawAnchor.Left, Text, new Vector2(posBar.X, position.Y), new Vector2(0, Config.Size.Y / 2), ImGui.ColorConvertFloat4ToU32(Config.NameTextColor), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), drawList, 5, 0);
                            }

                            // Text: Duration
                            if (Config.ShowDuration)
                            {
                                Helpers.DrawHelper.DrawAnchoredText("MyriadProLightCond_18", Config.NameTextextStyle, DrawAnchor.Right, Helpers.DrawHelper.FormatDuration(Config.ShowDurationRemaining ? Remaining : Elapsed, Config.MillisecondsThreshold, false), position, new Vector2(Config.Size.X, Config.Size.Y / 2), ImGui.ColorConvertFloat4ToU32(Config.NameTextColor), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), drawList, -5, 0);
                            }
                        }
                        break;

                    case BarManagerStyle.Classic:
                        { 
                            Vector2 posBar = position;
                            Vector2 sizeBar = Config.Size;

                            // Border
                            if (Config.BorderSize > 0) {
                                drawList.AddRect(position, position + Config.Size, ImGui.ColorConvertFloat4ToU32(Config.BorderColor), 0, ImDrawFlags.None, Config.BorderSize);
                                posBar.X += Config.BorderSize;
                                posBar.Y += Config.BorderSize;
                                sizeBar.X -= 2 * Config.BorderSize;
                                sizeBar.Y -= 2 * Config.BorderSize;
                            }

                            // Icon
                            if (Icon != null)
                            {
                                Vector2 posIcon = new(position.X + Config.BorderSize, position.Y + Config.BorderSize);
                                Vector2 sizeIcon = new(Config.Size.Y - 2 * Config.BorderSize, Config.Size.Y - 2 * Config.BorderSize);
                                (Vector2 uv0, Vector2 uv1) = Helpers.DrawHelper.GetTexCoordinates(sizeIcon);

                                drawList.AddRectFilled(posIcon, posIcon + sizeIcon, ImGui.ColorConvertFloat4ToU32(Config.BackgroundColor), 0);
                                drawList.AddImage(Icon.ImGuiHandle, posIcon, posIcon + sizeIcon, uv0, uv1, ImGui.ColorConvertFloat4ToU32(Vector4.One));

                                posBar.X += sizeIcon.X;
                                sizeBar.X -= sizeIcon.X;

                                // Border
                                if (Config.BorderSize > 0) {
                                    Vector2 posIconBorder = new(posIcon.X + sizeIcon.X, posIcon.Y);
                                    drawList.AddLine(posIconBorder, posIconBorder.AddY(sizeIcon.Y), ImGui.ColorConvertFloat4ToU32(Config.BorderColor), Config.BorderSize);
                                    posBar.X += Config.BorderSize;
                                    sizeBar.X -= Config.BorderSize;
                                }
                            }

                            // Bar BG
                            drawList.AddRectFilled(posBar, posBar + sizeBar, ImGui.ColorConvertFloat4ToU32(Config.BackgroundColor), 0);

                            // Bar
                            float fillPercent = (Config.FillInverted ? (float)Remaining : (float)Elapsed) / (float)Duration;
                            Vector2 sizeBarFilled = new(Config.FillDirection.IsHorizontal() ? sizeBar.X * fillPercent : sizeBar.X, !Config.FillDirection.IsHorizontal() ? sizeBar.Y * fillPercent : sizeBar.Y);
                            drawList.AddRectFilled(posBar, posBar + sizeBarFilled, ImGui.ColorConvertFloat4ToU32(Config.FillColor), 0);

                            // Text: Name
                            if (Text != null)
                            {
                                Helpers.DrawHelper.DrawAnchoredText("MyriadProLightCond_18", Config.NameTextextStyle, DrawAnchor.Left, Text, posBar, new Vector2(0, sizeBar.Y / 2), ImGui.ColorConvertFloat4ToU32(Config.NameTextColor), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), drawList, 4, 0);
                            }

                            // Text: Duration
                            if (Config.ShowDuration)
                            {
                                Helpers.DrawHelper.DrawAnchoredText("MyriadProLightCond_18", Config.NameTextextStyle, DrawAnchor.Right, Helpers.DrawHelper.FormatDuration(Config.ShowDurationRemaining ? Remaining : Elapsed, Config.MillisecondsThreshold, false), posBar, new Vector2(sizeBar.X, sizeBar.Y / 2), ImGui.ColorConvertFloat4ToU32(Config.NameTextColor), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), drawList, -4, 0);
                            }
                        }
                        break;

                    default:
                        Helpers.DrawHelper.DrawPlaceholder(Text ?? string.Empty, position, Config.Size, 1, drawList);
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
                return;
            }
        }
        #endregion
    }
}