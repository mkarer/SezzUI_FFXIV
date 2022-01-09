// https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Action.csv
// https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Status.csv

using System;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;

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

        /// <summary>
        /// Disables displaying big status duration when no cooldown is specified.
        /// </summary>
        NoStatusCooldownDisplay = 1L << 3,
    }

    public class Icon : IDisposable
    {
        public Bar Parent { get { return _parent; } }
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
                        bool useLocalTexture = false;
                        try
                        {
                            string path = Plugin.AssemblyLocation + $"Media\\Icons\\{action.Icon / 1000 * 1000:000000}\\{action.Icon:000000}.png";
                            if (System.IO.File.Exists(path))
                            {
                                _texture = Helpers.ImageCache.Instance.GetImageFromPath(path);
                                if (_texture != null)
                                {
                                    useLocalTexture = true;
                                    (_iconUV0, _iconUV1) = Helpers.DrawHelper.GetTexCoordinates(new Vector2(_texture.Width, _texture.Height), (_clipOffset != null) ? (float)_clipOffset : 0);
                                }
                            }
                        }
                        catch { }

                        if (!useLocalTexture)
                        {
                            _texture = DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId(action.Icon);
                        }
                    }
                }

                _textureActionId = value;
            }
        }
        private uint? _textureActionId;

        public uint? TextureStatusId
        {
            get { return _textureStatusId; }
            set
            {
                _textureStatusId = value;
      
                if (value != null)
                {
                    LuminaStatus? status = Helpers.SpellHelper.GetStatus((uint)value);
                    if (status != null)
                    {
                        bool useLocalTexture = false;
                        try
                        {
                            string path = Plugin.AssemblyLocation + $"Media\\Icons\\{status.Icon / 1000 * 1000:000000}\\{status.Icon:000000}.png";
                            if (System.IO.File.Exists(path))
                            {
                                _texture = Helpers.ImageCache.Instance.GetImageFromPath(path);
                                if (_texture != null)
								{
                                    useLocalTexture = true;
                                    (_iconUV0, _iconUV1) = Helpers.DrawHelper.GetTexCoordinates(new Vector2(_texture.Width, _texture.Height), 0);
                                }
                            }
                        }
                        catch { }

                        if (!useLocalTexture)
                        {
                            _texture = DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId(status.Icon);
                            IconClipOffset = (_clipOffset != null) ? (float)_clipOffset : 0;
                        }
                    }
                }
            }
        }
        private uint? _textureStatusId;

        private TextureWrap? _texture;

        /// <summary>
        /// Action ID that will be used to display cooldown spiral and text.
        /// </summary>
        public uint? CooldownActionId;
        public float CooldownWarningThreshold = 7f;

        /// <summary>
        /// Status (NOT Action) ID for tracking the duration of a buff/debuff on a unit.
        /// By default this also shows a small progress bar unless there is no cooldown action to track.
        /// When not specifying a cooldown action the duration will be displayed like a cooldown.
        /// </summary>
        public uint? StatusId;
        public uint[]? StatusIds;
        public bool StatusSourcePlayer = true;
        public bool StatusRequired = false;

        /// <summary>
        /// Status (NOT Action) ID used for displaying stacks only.
        /// Overrides other stack counters!
        /// </summary>
        public uint? StacksStatusId;

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
        public float[]? MaxStatusDurations;
        public Enums.Unit? StatusTarget;
        public float StatusWarningThreshold = 7f;
        public Func<(float, float)>? CustomDuration;

        public uint? GlowBorderStatusId;
        public uint[]? GlowBorderStatusIds;

        /// <summary>
        /// Additional status check when GlowBorderUsable is true AND GlowBorderInvertCheck is true AND GlowBorderStatusId(s) is set.
        /// Only show the glowing border when this status is found on the player (in addition to all the other conditions).
        /// </summary>
        public uint? GlowBorderStatusIdForced;

        /// <summary>
        /// Show glowing border if the action is usable instead of checking for a status.
        /// </summary>
        public bool GlowBorderUsable = false;

        /// <summary>
        /// Show glowing border if the status is not found/the action is not usable.
        /// </summary>
        public bool GlowBorderInvertCheck = false;

        public Helpers.JobsHelper.PowerType? RequiredPowerType;
        public int? RequiredPowerAmount;
        public int? RequiredPowerAmountMax;
        public Func<bool>? CustomPowerCondition;
        public Helpers.JobsHelper.PowerType? StacksPowerType;

        /// <summary>
        /// Required job level to show icon.
        /// </summary>
        public byte Level = 0;

        /// <summary>
        /// Action can only be executed while in combat.
        /// </summary>
        public bool RequiresCombat = false;
        public bool RequiresPet = false;
        public Func<bool>? CustomCondition;

        /// <summary>
        /// If the icon size is not 1:1 the visible area will be cropped.
        /// You can specify a negative value to moves the visible area up or left, or a positive value to move it down or right.
        /// </summary>
        public float IconClipOffset {
            set
            {
                _clipOffset = value;
                if (_parent.IconSize.X == _parent.IconSize.Y && _textureStatusId == null) { return; }
                (_iconUV0, _iconUV1) = Helpers.DrawHelper.GetTexCoordinates(_parent.IconSize, value, _textureStatusId != null);
            }
        }
        private Vector2? _iconUV0;
        private Vector2? _iconUV1;
        private float? _clipOffset;

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

        /// <summary>
        /// Checks level, cooldown action and texture action (in this priority) to decide if this should be shown.
        /// </summary>
        /// <returns></returns>
        public bool ShouldShow()
        {
            if (Level > 0)
            {
                return Level >= (Plugin.ClientState.LocalPlayer?.Level ?? 0);
            }
            else if (CooldownActionId != null)
            {
                return Helpers.JobsHelper.IsActionUnlocked((uint)CooldownActionId);
            }
            else if (TextureActionId != null)
            {
                return Helpers.JobsHelper.IsActionUnlocked((uint)TextureActionId);
            }
            else
            {
                return true;
            }
        }

        public void Draw(Vector2 pos, Vector2 size, Animator.Animator animator, ImDrawListPtr drawList)
        {
            PlayerCharacter player = Plugin.ClientState.LocalPlayer!;

            IconState newState = _state;
            float cooldownTextRemaining = 0;
            float cooldownSpiralTotal = 0;
            float cooldownSpiralRemaining = 0;
            short chargesTextAmount = -1;
            bool displayGlow = false;
            float progressBarCurrent = 0;
            float progressBarTotal = 0;
            float progressBarTextRemaining = 0;
            bool hasEnoughResources = true;

            bool failedCombatCondition = RequiresCombat && !EventManager.Combat.IsInCombat(false);
            bool failedPetCondition = RequiresPet && !Plugin.BuddyList.PetBuddyPresent;
            bool failedCustomCondition = CustomCondition != null && !CustomCondition();
            bool failedInitialCondition = failedCombatCondition || failedPetCondition || failedCustomCondition;

            Vector2 posInside = pos + Vector2.One;
            Vector2 sizeInside = size - 2 * Vector2.One;

            // --------------------------------------------------------------------------------
            // Conditions
            // --------------------------------------------------------------------------------

            // Initial
            if (failedInitialCondition)
            {
                newState = IconState.FadedOut;
            }

            // Cooldown + Charges
            if (CooldownActionId != null)
			{
                Helpers.CooldownData cooldown = Helpers.SpellHelper.GetCooldownData((uint)CooldownActionId);

                if (cooldown.CooldownRemaining > 0)
				{
                    if (!failedInitialCondition)
                    {
                        newState = cooldown.CooldownRemaining > CooldownWarningThreshold ? IconState.FadedOut : IconState.Soon;
                    }

                    cooldownTextRemaining = cooldown.CooldownRemaining;
                    cooldownSpiralRemaining = cooldown.CooldownRemaining;
                    cooldownSpiralTotal = cooldown.CooldownPerCharge;
                }
                else if (!failedInitialCondition)
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
            if ((StatusId != null || StatusIds != null) && StatusTarget != null && (MaxStatusDuration != null || MaxStatusDurations != null))
			{
                bool shouldShowStatusBar = !Features.HasFlag(IconFeatures.NoStatusBar);
                bool shouldShowStatusAsCooldown = (CooldownActionId == null && !Features.HasFlag(IconFeatures.NoStatusCooldownDisplay));
                Status? status = null;

                if (StatusId != null)
                {
                    status = (StatusTarget is Enums.Unit.TargetOrPlayer) ?
                        Helpers.SpellHelper.GetStatus((uint)StatusId, Enums.Unit.Target, StatusSourcePlayer) ?? Helpers.SpellHelper.GetStatus((uint)StatusId, Enums.Unit.Player, StatusSourcePlayer) :
                        Helpers.SpellHelper.GetStatus((uint)StatusId, (Enums.Unit)StatusTarget, StatusSourcePlayer);
                }
                else if (StatusIds != null)
                {
                    status = (StatusTarget is Enums.Unit.TargetOrPlayer) ?
                        Helpers.SpellHelper.GetStatus(StatusIds, Enums.Unit.Target, StatusSourcePlayer) ?? Helpers.SpellHelper.GetStatus(StatusIds, Enums.Unit.Player, StatusSourcePlayer) :
                        Helpers.SpellHelper.GetStatus(StatusIds, (Enums.Unit)StatusTarget, StatusSourcePlayer);
                }

                if (status != null)
				{
                    // Permanent is either really permanent or a status that hasn't ticked yet.
                    float duration = ((MaxStatusDuration ?? 0) == Constants.PERMANENT_STATUS_DURATION ? Constants.PERMANENT_STATUS_DURATION : Math.Max(Constants.PERMANENT_STATUS_DURATION, status.RemainingTime));
                    byte stacks = ((duration > 0 || duration == Constants.PERMANENT_STATUS_DURATION) && status!.GameData.MaxStacks > 1) ? status.StackCount : (byte)0;

                    float durationMax = 0f;
                    if (MaxStatusDuration != null)
                    {
                        durationMax = (float)MaxStatusDuration;
                    }
                    else if (StatusIds != null && MaxStatusDurations != null)
                    {
                        int index = Array.IndexOf(StatusIds, status!.StatusId);
                        if (index >= 0 && index < MaxStatusDurations.Length)
                        {
                            durationMax = MaxStatusDurations[index];
                        }
                    }

                    // State
                    if (shouldShowStatusAsCooldown && !failedInitialCondition)
                    {
                        newState = (duration <= StatusWarningThreshold && duration >= 0 ? IconState.Soon : IconState.FadedOut);
                    }

                    // Progress Bar
                    if (shouldShowStatusBar)
					{
                        progressBarCurrent = duration == Constants.PERMANENT_STATUS_DURATION ? 1 : duration;
                        progressBarTotal = duration == Constants.PERMANENT_STATUS_DURATION ? 1 : durationMax;

                        // Duration Text
                        if (!shouldShowStatusAsCooldown && !Features.HasFlag(IconFeatures.NoStatusBarText) && duration != Constants.PERMANENT_STATUS_DURATION)
                        {
                            progressBarTextRemaining = (duration > 3 ? (int)Math.Ceiling(duration) : duration);
                        }
                    }

                    // Cooldown Spiral
                    if (shouldShowStatusAsCooldown && duration >= 0)
                    {
                        cooldownTextRemaining = duration;
                        cooldownSpiralRemaining = duration;
                        cooldownSpiralTotal = durationMax;
                    }

                    // Stacks
                    if (chargesTextAmount == Constants.PERMANENT_STATUS_DURATION && stacks > 0)
					{
                        chargesTextAmount = stacks;
                    }
                }
                else if (shouldShowStatusAsCooldown && !failedInitialCondition)
                {
                    newState = StatusRequired ? IconState.FadedOut : IconState.Ready;
                }
                else if (StatusRequired)
                {
                    newState = IconState.FadedOut;
                }
            }

            if (StacksStatusId != null)
            {
                Status? status = Helpers.SpellHelper.GetStatus((uint)StacksStatusId, Enums.Unit.Player);
                if (status != null)
                {
                    float duration = Math.Abs(status?.RemainingTime ?? 0f);
                    byte stacks = (duration > 0 && status!.GameData.MaxStacks > 1) ? status.StackCount : (byte)0;
                    if (stacks > 0)
                    {
                        chargesTextAmount = stacks;
                    }
                }
            }

            if (CustomDuration != null)
            {
                (float duration, float durationMax) = CustomDuration();

                progressBarCurrent = duration == Constants.PERMANENT_STATUS_DURATION ? 1 : duration;
                progressBarTotal = duration == Constants.PERMANENT_STATUS_DURATION ? 1 : durationMax;

                // Duration Text
                if (duration != Constants.PERMANENT_STATUS_DURATION)
                {
                    progressBarTextRemaining = (duration > 3 ? (int)Math.Ceiling(duration) : duration);
                }
            }

            // No conditions...
            if (!failedInitialCondition && CooldownActionId == null && StatusId == null && StatusIds == null)
            {
                newState = IconState.Ready;
            }

            // Resources
            if (RequiredPowerType != null)
			{
                (int current, int max) = Helpers.JobsHelper.GetPower((Helpers.JobsHelper.PowerType)RequiredPowerType);
                if (RequiredPowerAmountMax != null)
                {
                    hasEnoughResources = RequiredPowerAmount != null ?
                        (current >= RequiredPowerAmount && current <= RequiredPowerAmountMax):
                        (current <= RequiredPowerAmountMax);
                }
                else
                {
                    hasEnoughResources = (current >= (RequiredPowerAmount ?? 1));
                }

                if (StacksPowerType != null && current > 0)
                {
                    chargesTextAmount = (short)Math.Floor((float)current);
                }
            } else if (CustomPowerCondition != null)
            {
                hasEnoughResources = CustomPowerCondition();
            }

            if (newState == IconState.Ready && !hasEnoughResources)
			{
                newState = IconState.ReadyOutOfResources;
			}

            if (RequiredPowerType == null && StacksPowerType != null)
            {
                (int current, int max) = Helpers.JobsHelper.GetPower((Helpers.JobsHelper.PowerType)StacksPowerType);
                if (current > 0)
                {
                    chargesTextAmount = (short)Math.Floor((float)current);
                }
            }

            // Glow
            if (newState == IconState.Ready || Features.HasFlag(IconFeatures.GlowIgnoresState))
            {
                if (GlowBorderUsable)
				{
                    if (!GlowBorderInvertCheck)
                    {
                        displayGlow = true;
                    }
                }
                else if (GlowBorderStatusId != null)
				{
                    Status? status = Helpers.SpellHelper.GetStatus((uint)GlowBorderStatusId, Enums.Unit.Player);
                    float remaining = status?.RemainingTime ?? 0f;
                    displayGlow = (status != null && (remaining == Constants.PERMANENT_STATUS_DURATION || remaining > 0));
                    if (GlowBorderInvertCheck) { displayGlow = !displayGlow; };
                }
                else if (GlowBorderStatusIds != null)
				{
                    Status? status = Helpers.SpellHelper.GetStatus(GlowBorderStatusIds, Enums.Unit.Player);
                    float remaining = status?.RemainingTime ?? 0f;
                    displayGlow = (status != null && (remaining == Constants.PERMANENT_STATUS_DURATION || remaining > 0));
                    if (GlowBorderInvertCheck) { displayGlow = !displayGlow; };
                }
            } else if (GlowBorderUsable && GlowBorderInvertCheck && newState != IconState.Ready)
            {
                if (GlowBorderStatusId != null)
                {
                    bool hasForcedStatus = GlowBorderStatusIdForced == null || Helpers.SpellHelper.GetStatus((uint)GlowBorderStatusIdForced, Enums.Unit.Player) != null;
                    if (hasForcedStatus)
                    {
                        Status? status = Helpers.SpellHelper.GetStatus((uint)GlowBorderStatusId, Enums.Unit.Player);
                        displayGlow = (status == null);
                    }
                }
                else if (GlowBorderStatusIds != null)
                {
                    bool hasForcedStatus = GlowBorderStatusIdForced == null || Helpers.SpellHelper.GetStatus((uint)GlowBorderStatusIdForced, Enums.Unit.Player) != null;
                    if (hasForcedStatus)
                    {
                        Status? status = Helpers.SpellHelper.GetStatus(GlowBorderStatusIds, Enums.Unit.Player);
                        displayGlow = (status == null);
                    }
                }
                else
                {
                    displayGlow = true;
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
                drawList.AddImage(_texture.ImGuiHandle, posInside, posInside + sizeInside, _iconUV0 != null ? (Vector2)_iconUV0 : _parent.IconUV0, _iconUV1 != null ? (Vector2)_iconUV1 : _parent.IconUV1, ImGui.ColorConvertFloat4ToU32(_animatorTexture.Data.Color.AddTransparency(animator.Data.Opacity)));
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
                Helpers.DrawHelper.DrawAnchoredText("MyriadProLightCond_14", Enums.TextStyle.Outline, Enums.DrawAnchor.BottomRight, chargesTextAmount.ToString(), posInside, sizeInside, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, animator.Data.Opacity)), drawList, -2, -1);
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
                    ImGui.ColorConvertFloat4ToU32(Parent.Parent.AccentColor.AddTransparency(animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(Defaults.IconBarBGColor.AddTransparency(animator.Data.Opacity)),
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

        ~Icon()
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
    }
}
