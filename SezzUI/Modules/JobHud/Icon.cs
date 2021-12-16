// https://github.com/xivapi/ffxiv-datamining/blob/master/csv/Status.csv

using System;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using XIVAuras.Helpers;

namespace SezzUI.Modules.JobHud
{
    [Flags]
    public enum IconFeatures : long
    {
        Default = 0,

        /// <summary>
        /// Disables the small progress bar below the actual icon that displays the uptime of a buff/debuff.
        /// </summary>
        NoStatusBar = 1L << 0,

        /// <summary>
        /// Disables the duration text in the middle of the small status progress bar.
        /// </summary>
        NoStatusBarText = 1L << 1,
    }

    class Icon : IDisposable
    {
        public IconFeatures Features = IconFeatures.Default;

        /// <summary>
        /// Action ID that will be used to lookup the icon texture.
        /// </summary>
		public uint? TextureActionId
        {
            get { return _textureActionId; }
            set
            {
                if (value != null)
				{
                    LuminaAction? action = Helpers.SpellHelper.GetAction((uint)value);
                    if (action is not null)
                    {
                        _texture = DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId(action.Icon);
                    }
                }

                _textureActionId = value;
            }
        }
        private uint? _textureActionId;
        private TextureWrap? _texture;

        /// <summary>
        /// Action ID that will be used to display cooldown spiral and text.
        /// </summary>
		public uint? CooldownActionId;

        /// <summary>
        /// Status (NOT Action) ID for tracking the duration of a buff/debuff on a unit.
        /// By default this also shows a small progress bar unless there is no cooldown action to track.
        /// When not specifying a cooldown action the duration will be displayed like a cooldown.
        /// </summary>
        public uint? StatusId;

        /// <summary>
        /// Action ID to lookup Status ID by name.
        /// </summary>
        public uint? StatusActionId
        {
            get { return _statusActionId; }
            set
            {
                _statusActionId = value;

                if (value != null)
                {
                    LuminaStatus? status = Helpers.SpellHelper.GetStatusByAction((uint)value);
                    if (status is not null)
                    {
                        StatusId = status.RowId;
                        Dalamud.Logging.PluginLog.Debug($"Found matching status for {value}: {StatusId}");
                    }
                }
            }
        }
        private uint? _statusActionId;
        public float? MaxStatusDuration;
        public Enums.Unit? StatusTarget;

        /// <summary>
        /// Required job level to show icon.
        /// </summary>
        public byte Level = 1;

        public Icon()
		{
		}

        public void Draw(Vector2 pos, Vector2 size, Animator.Animator animator, ImDrawListPtr drawList)
        {
            PlayerCharacter? player = Service.ClientState.LocalPlayer;
            if (player == null) return; // Stupid IDE, we're not drawing this without a player anyways...

            Vector2 posInside = pos + Vector2.One;
            Vector2 sizeInside = size - 2 * Vector2.One;

            // Backdrop + Icon Texture
            if (_texture != null)
			{
                Helpers.DrawHelper.DrawBackdrop(pos, size, animator.Data.Opacity, drawList);
                (Vector2 uv0, Vector2 uv1) = DelvUI.Helpers.DrawHelper.GetTexCoordinates(_texture, size, false);
                drawList.AddImage(_texture.ImGuiHandle, posInside, posInside + sizeInside, uv0, uv1, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, animator.Data.Opacity)));
            }
            else
			{
                Helpers.DrawHelper.DrawPlaceholder("X", pos, size, animator.Data.Opacity, drawList);
            }

            // Cooldown + Charges
            if (CooldownActionId != null)
			{
                int maxCharges = DelvUI.Helpers.SpellHelper.Instance.GetMaxCharges((uint)CooldownActionId, player.Level);
                int currentCharges = DelvUI.Helpers.SpellHelper.Instance.GetStackCount(maxCharges, (uint)CooldownActionId);
                float chargeTime = DelvUI.Helpers.SpellHelper.Instance.GetRecastTime((uint)CooldownActionId) / maxCharges;
                float cooldown = chargeTime != 0
                    ? DelvUI.Helpers.SpellHelper.Instance.GetSpellCooldown((uint)CooldownActionId) % chargeTime
                    : chargeTime;

                if (cooldown > 0)
				{
                    // Spiral
                    DrawProgressSwipe(posInside, sizeInside, cooldown, chargeTime, animator.Data.Opacity, drawList);

                    // Text
                    // https://stackoverflow.com/questions/463642/what-is-the-best-way-to-convert-seconds-into-hourminutessecondsmilliseconds
                    int cooldownRounded = (int)Math.Ceiling(cooldown);
                    int seconds = cooldownRounded % 60;
                    if (cooldownRounded >= 60)
					{
                        int minutes = (cooldownRounded % 3600) / 60;
                        Helpers.DrawHelper.DrawCenteredOutlineText("MyriadProLightCond_20", String.Format("{0:D1}:{1:D2}", minutes, seconds), posInside, sizeInside, ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList);
                    }
                    else if (cooldown > 3)
					{
                        Helpers.DrawHelper.DrawCenteredOutlineText("MyriadProLightCond_20", String.Format("{0:D1}", seconds), posInside, sizeInside, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList);
                    }
                    else
					{
                        Helpers.DrawHelper.DrawCenteredOutlineText("MyriadProLightCond_20", cooldown.ToString("0.0", Plugin.NumberFormatInfo), posInside, sizeInside, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList);
                    }
                }

                if (maxCharges > 1)
				{
                    Helpers.DrawHelper.DrawAnchoredText("MyriadProLightCond_14", Enums.TextStyle.Outline, DelvUI.Enums.DrawAnchor.BottomRight, currentCharges.ToString(), posInside, sizeInside, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList, -2, -1);
                }
            }

            // Status
            if (StatusId != null && StatusTarget != null && MaxStatusDuration != null)
			{
                bool shouldShowStatusBar = (CooldownActionId != null && !Features.HasFlag(IconFeatures.NoStatusBar));
                bool shouldShowStatusAsCooldown = (CooldownActionId == null);

                GameObject? target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
                GameObject? actor = StatusTarget switch
                {
                    Enums.Unit.Player => player,
                    Enums.Unit.Target => target,
                    Enums.Unit.TargetOfTarget => DelvUI.Helpers.Utils.FindTargetOfTarget(player, target, Plugin.ObjectTable),
                    Enums.Unit.FocusTarget => Plugin.TargetManager.FocusTarget,
                    _ => null
                };

                if (actor is BattleChara chara)
				{
                    foreach (var status in chara.StatusList)
					{
                        if (status is not null && status.StatusId == StatusId && status.SourceID == player.ObjectId)
						{
                            float duration = status?.RemainingTime ?? -1;
                            // Progress Bar
                            if (shouldShowStatusBar)
							{
                                Vector2 progressSize = new Vector2(sizeInside.X, 2);
                                Vector2 progressPos = new Vector2(posInside.X, posInside.Y + sizeInside.Y - progressSize.Y);

                                Vector2 linePos = progressPos.AddY(-1);
                                Vector2 lineSize = progressSize.AddY(-progressSize.Y);
                                drawList.AddLine(linePos, linePos + lineSize, ImGui.ColorConvertFloat4ToU32(Defaults.IconBarSeparatorColor.AddTransparency(animator.Data.Opacity)), 1);

                                Helpers.DrawHelper.DrawProgressBar(progressPos, progressSize, 0, (float)MaxStatusDuration, duration,
                                    ImGui.ColorConvertFloat4ToU32(Defaults.IconBarColor.AddTransparency(animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(Defaults.IconBarBGColor.AddTransparency(animator.Data.Opacity)),
                                    drawList);

                                // Duration Text
                                if (!Features.HasFlag(IconFeatures.NoStatusBarText))
                                {
                                    if (duration > 3)
                                    {
                                        Helpers.DrawHelper.DrawCenteredOutlineText("MyriadProLightCond_12", String.Format("{0:D1}", (int)Math.Ceiling(duration)), linePos, lineSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList);
                                    }
                                    else if (duration > 0)
                                    {
                                        Helpers.DrawHelper.DrawCenteredOutlineText("MyriadProLightCond_12", duration.ToString("0.0", Plugin.NumberFormatInfo), linePos, lineSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList);
                                    }
                                }
                            }

                            // Cooldown Spiral
                            if (shouldShowStatusAsCooldown)
                            {
                            }
                            break;
						}
                    }
                }
            }
        }

        private void DrawProgressSwipe(Vector2 pos, Vector2 size, float triggeredValue, float startValue, float alpha, ImDrawListPtr drawList)
        {
            // TODO: HUD Clipping
            if (startValue > 0)
            {
                bool invert = false; // this.IconStyleConfig.InvertSwipe;
                float percent = (invert ? 0 : 1) - (startValue - triggeredValue) / startValue;

                float radius = (float)Math.Sqrt(Math.Pow(Math.Max(size.X, size.Y), 2) * 2) / 2f;
                float startAngle = -(float)Math.PI / 2;
                float endAngle = startAngle - 2f * (float)Math.PI * percent;

                ImGui.PushClipRect(pos, pos + size, false);
                drawList.PathArcTo(pos + size / 2, radius / 2, startAngle, endAngle, (int)(100f * Math.Abs(percent)));
                uint progressAlpha = (uint)(0.6f * 255 * alpha) << 24; //(uint)(this.IconStyleConfig.ProgressSwipeOpacity * 255 * alpha) << 24;
                drawList.PathStroke(progressAlpha, ImDrawFlags.None, radius);
                if (true && triggeredValue != 0) // if (this.IconStyleConfig.ShowSwipeLines && triggeredValue != 0)
                {
                    Vector2 vec = new Vector2((float)Math.Cos(endAngle), (float)Math.Sin(endAngle));
                    Vector2 start = pos + size / 2;
                    Vector2 end = start + vec * radius;
                    float thickness = 1;// this.IconStyleConfig.ProgressLineThickness;
                    Vector4 swipeLineColor = new Vector4(1, 1, 1, 0.3f * alpha); //this.IconStyleConfig.ProgressLineColor.Vector.AddTransparency(alpha);
                    uint color = ImGui.ColorConvertFloat4ToU32(swipeLineColor);

                    drawList.AddLine(start, end, color, thickness);
                    drawList.AddLine(start, new(pos.X + size.X / 2, pos.Y), color, thickness);
                    drawList.AddCircleFilled(start + new Vector2(thickness / 4, thickness / 4), thickness / 2, color);
                }

                ImGui.PopClipRect();
            }
        }

        public void Dispose()
        {
        }
    }
}
