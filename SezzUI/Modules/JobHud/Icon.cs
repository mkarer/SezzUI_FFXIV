﻿// https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Action.csv
// https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Status.csv

using System;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
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

        /// <summary>
        /// Usually only icons that are ready/usable glow, use this to ignore the button state and allow it glow whenever the status is found.
        /// Applies only when GlowBorderStatusId or GlowBorderStatusIds is defined.
        /// </summary>
        GlowIgnoresState = 1L << 2,
    }

    public sealed class Icon : IDisposable
    {
        private Bar _parent;
        private Animator.Animator _animatorBorder;
        private Animator.Animator _animatorTexture;
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
                    uint actionIdAdjusted = DelvUI.Helpers.SpellHelper.Instance.GetSpellActionId((uint)value);
                    LuminaAction? action = Helpers.SpellHelper.GetAction(actionIdAdjusted);
                    if (action != null)
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
                    uint actionIdAdjusted = DelvUI.Helpers.SpellHelper.Instance.GetSpellActionId((uint)value);
                    LuminaStatus? status = Helpers.SpellHelper.GetStatusByAction(actionIdAdjusted);
                    if (status != null)
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

        public bool GlowBorderUsable = false;
        public uint? GlowBorderStatusId;
        public uint[]? GlowBorderStatusIds;

        public Helpers.JobsHelper.PowerType? RequiredPowerType;
        public int? RequiredPowerAmount;

        /// <summary>
        /// Required job level to show icon.
        /// </summary>
        public byte Level = 1;

        private IconState _state = IconState.Ready;

        public Icon(Bar parent)
		{
            _parent = parent;
            _animatorBorder = new();
            _animatorBorder.Timelines.OnShow.Data.DefaultColor = Defaults.StateColors[_state].Border;
            _animatorBorder.Timelines.OnShow.Add(new Animator.ColorAnimation(_animatorBorder.Timelines.OnShow.Data.DefaultColor, _animatorBorder.Timelines.OnShow.Data.DefaultColor, 100));
            _animatorBorder.Data.Reset(_animatorBorder.Timelines.OnShow.Data);
            _animatorTexture = new();
            _animatorTexture.Timelines.OnShow.Data.DefaultColor = Defaults.StateColors[_state].Icon;
            _animatorTexture.Timelines.OnShow.Add(new Animator.ColorAnimation(_animatorTexture.Timelines.OnShow.Data.DefaultColor, _animatorTexture.Timelines.OnShow.Data.DefaultColor, 100));
            _animatorTexture.Data.Reset(_animatorTexture.Timelines.OnShow.Data);
        }

        public void Draw(Vector2 pos, Vector2 size, Animator.Animator animator, ImDrawListPtr drawList)
        {
            PlayerCharacter? player = Service.ClientState.LocalPlayer;
            if (player == null) return; // Stupid IDE, we're not drawing this without a player anyways...

            IconState newState = _state;
            float cooldownTextRemaining = 0;
            float cooldownSpiralTotal = 0;
            float cooldownSpiralRemaining = 0;
            short chargesTextAmount = -1;
            bool displayGlow = false;
            float progressBarTotal = 0;
            float progressBarCurrent = 0;
            float progressBarTextRemaining = 0;
            bool hasEnoughResources = true;

            Vector2 posInside = pos + Vector2.One;
            Vector2 sizeInside = size - 2 * Vector2.One;

            // --------------------------------------------------------------------------------
            // Conditions
            // --------------------------------------------------------------------------------

            // Cooldown + Charges
            // Will be used as IconState by default.
            if (CooldownActionId != null)
			{
                Helpers.CooldownData cooldown = Helpers.SpellHelper.GetCooldownData((uint)CooldownActionId);

                if (cooldown.CooldownRemaining > 0)
				{
                    newState = cooldown.CooldownRemaining > 7 ? IconState.FadedOut : IconState.Soon;

                    cooldownTextRemaining = cooldown.CooldownRemaining;
                    cooldownSpiralRemaining = cooldown.CooldownRemaining;
                    cooldownSpiralTotal = cooldown.CooldownPerCharge;
                }
                else
				{
                    newState = IconState.Ready;
                }

                if (cooldown.ChargesMax > 1)
				{
                    chargesTextAmount = (short)cooldown.ChargesCurrent;
                }
            }

            // Status
            // Will be used as IconState if not checking any cooldowns.
            if (StatusId != null && StatusTarget != null && MaxStatusDuration != null)
			{
                bool shouldShowStatusBar = !Features.HasFlag(IconFeatures.NoStatusBar);
                bool shouldShowStatusAsCooldown = (CooldownActionId == null);
                Status? status = Helpers.SpellHelper.GetStatus((uint)StatusId, (Enums.Unit)StatusTarget);

                if (status != null && status.StatusId == StatusId && status.SourceID == player.ObjectId)
				{
                    float duration = status?.RemainingTime ?? -1;
                    byte stacks = (duration > 0 && status != null && status.GameData.MaxStacks > 1) ? status.StackCount : (byte)0;

                    // State
                    if (shouldShowStatusAsCooldown)
                    {
                        newState = (duration <= 7 ? IconState.Soon : IconState.FadedOut);
                    }

                    // Progress Bar
                    if (shouldShowStatusBar)
					{
                        progressBarTotal = (float)MaxStatusDuration;
                        progressBarCurrent = duration;

                        // Duration Text
                        if (!shouldShowStatusAsCooldown && !Features.HasFlag(IconFeatures.NoStatusBarText))
                        {
                            progressBarTextRemaining = (duration > 3 ? (int)Math.Ceiling(duration) : duration);
                        }
                    }

                    // Cooldown Spiral
                    if (shouldShowStatusAsCooldown)
                    {
                        cooldownTextRemaining = duration;
                        cooldownSpiralRemaining = duration;
                        cooldownSpiralTotal = (float)MaxStatusDuration;
                    }

                    // Stacks
                    if (chargesTextAmount == -1 && stacks > 0)
					{
                        chargesTextAmount = stacks;
                    }
                }
                else if (shouldShowStatusAsCooldown)
                {
                    newState = IconState.Ready;
                }
            }

            // Resources
            if (RequiredPowerType != null && RequiredPowerAmount != null)
			{
                (int current, int max) = Helpers.JobsHelper.GetPower((Helpers.JobsHelper.PowerType)RequiredPowerType);
                hasEnoughResources = (current >= RequiredPowerAmount);
			}

            if (newState == IconState.Ready && !hasEnoughResources)
			{
                newState = IconState.ReadyOutOfResources;
			}

            // Glow
            if (newState == IconState.Ready || Features.HasFlag(IconFeatures.GlowIgnoresState))
            {
                if (GlowBorderUsable)
				{
                    displayGlow = true;
                }
                else if (GlowBorderStatusId != null)
				{
                    Status? status = Helpers.SpellHelper.GetStatus((uint)GlowBorderStatusId, Enums.Unit.Player);
                    displayGlow = (status != null && (status?.RemainingTime ?? -1) > 0);
                }
                else if (GlowBorderStatusIds != null)
				{
                    Status? status = Helpers.SpellHelper.GetStatus(GlowBorderStatusIds, Enums.Unit.Player);
                    displayGlow = (status != null && (status?.RemainingTime ?? -1) > 0);
                }
            }

            // --------------------------------------------------------------------------------
            // Draw
            // --------------------------------------------------------------------------------

            // Backdrop + Icon Texture
            if (_texture != null)
            {
                if (newState != _state)
				{
                    Animator.ColorAnimation anim1 = (Animator.ColorAnimation)_animatorBorder.Timelines.OnShow.Animations[0];
                    anim1.ColorFrom = _animatorBorder.Data.Color;
                    anim1.ColorTo = Defaults.StateColors[newState].Border;
                    _animatorBorder.Timelines.OnShow.Data.DefaultColor = anim1.ColorFrom;
                    _animatorBorder.Timelines.Loop.Data.DefaultColor = anim1.ColorTo;
                    _animatorBorder.Animate();

                    Animator.ColorAnimation anim2 = (Animator.ColorAnimation)_animatorTexture.Timelines.OnShow.Animations[0];
                    anim2.ColorFrom = _animatorTexture.Data.Color;
                    anim2.ColorTo = Defaults.StateColors[newState].Icon;
                    _animatorTexture.Timelines.OnShow.Data.DefaultColor = anim2.ColorFrom;
                    _animatorTexture.Timelines.Loop.Data.DefaultColor = anim2.ColorTo;
                    _animatorTexture.Animate();
                }

                _animatorBorder.Update();
                _animatorTexture.Update();

                Helpers.DrawHelper.DrawBackdrop(pos, size, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(_animatorBorder.Data.Color.AddTransparency(animator.Data.Opacity)), drawList);
                drawList.AddImage(_texture.ImGuiHandle, posInside, posInside + sizeInside, _parent.IconUV0, _parent.IconUV1, ImGui.ColorConvertFloat4ToU32(_animatorTexture.Data.Color.AddTransparency(animator.Data.Opacity)));
            }
            else
            {
                Helpers.DrawHelper.DrawPlaceholder("?", pos, size, animator.Data.Opacity, drawList);
            }

            // Cooldown + Charges
            if (cooldownSpiralTotal > 0)
			{
                Helpers.DrawHelper.DrawProgressSwipe(posInside, sizeInside, cooldownSpiralRemaining, cooldownSpiralTotal, animator.Data.Opacity, drawList);
                Helpers.DrawHelper.DrawCooldownText(posInside, sizeInside, cooldownTextRemaining, drawList, "MyriadProLightCond_20", animator.Data.Opacity);
            }

            if (chargesTextAmount >= 0)
			{
                Helpers.DrawHelper.DrawAnchoredText("MyriadProLightCond_14", Enums.TextStyle.Outline, DelvUI.Enums.DrawAnchor.BottomRight, chargesTextAmount.ToString(), posInside, sizeInside, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList, -2, -1);
            }

            // Glow
            if (displayGlow)
			{
                // Testing fake glow using images...
                // https://github.com/ocornut/imgui/issues/4706
                // https://kovart.github.io/dashed-border-generator/

                uint n = 8; // number of textures to cycle through
                uint dur = 250; // duration of one full cycle
                float frametime = dur / n; // display duration of 1 single frame
                uint step = Math.Min(n, Math.Max(1, (uint)Math.Ceiling((uint)(animator.TimeElapsed % dur) / frametime)));
                string image = Plugin.AssemblyLocation + "Media\\Images\\Animations\\DashedRect38_" + step + ".png";

                TextureWrap? tex = Helpers.ImageCache.Instance.GetImageFromPath(image);
                if (tex != null)
                {
                    uint glowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.95f, 0.32f, animator.Data.Opacity));
                    drawList.AddImage(tex.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One, glowColor);
                }
                else
                {
                    uint glowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(206f / 255f, 1, 0, 1 * animator.Data.Opacity));
                    drawList.AddRect(pos - Vector2.One, pos + size + Vector2.One, glowColor, 0, ImDrawFlags.None, 1);
                }
            }

            // Status Progress Bar
            if (progressBarTotal > 0)
			{
                // Progress Bar
                Vector2 progressSize = new(sizeInside.X, 2);
                Vector2 progressPos = new(posInside.X, posInside.Y + sizeInside.Y - progressSize.Y);

                Vector2 linePos = progressPos.AddY(-1);
                Vector2 lineSize = progressSize.AddY(-progressSize.Y);
                drawList.AddLine(linePos, linePos + lineSize, ImGui.ColorConvertFloat4ToU32(Defaults.IconBarSeparatorColor.AddTransparency(animator.Data.Opacity)), 1);

                Helpers.DrawHelper.DrawProgressBar(progressPos, progressSize, 0, progressBarTotal, progressBarCurrent,
					ImGui.ColorConvertFloat4ToU32(Plugin.SezzUIPlugin.Modules.JobHud.AccentColor.AddTransparency(animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(Defaults.IconBarBGColor.AddTransparency(animator.Data.Opacity)),
                    drawList);

                // Duration Text
                if (progressBarTextRemaining > 0)
                {
                    Helpers.DrawHelper.DrawCooldownText(linePos, lineSize, progressBarTextRemaining, drawList, "MyriadProLightCond_12", animator.Data.Opacity);
                }
            }

            // Done
            _state = newState;
        }

        public void Dispose()
        {
        }
    }
}
