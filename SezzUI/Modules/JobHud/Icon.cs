﻿// https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Action.csv
// https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Status.csv

using System;
using System.Numerics;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;
using SezzUI.Enums;
using SezzUI.Game;
using SezzUI.Game.Events;
using SezzUI.Game.Events.Cooldown;
using SezzUI.Helper;
using SezzUI.Interface.Animation;
using SezzUI.Logging;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace SezzUI.Modules.JobHud;

[Flags]
public enum IconFeatures : long
{
	Default = 0,

	/// <summary>
	///     Disables the small progress bar below the actual icon that displays the uptime of a buff/debuff.
	/// </summary>
	NoStatusBar = 1L << 0,

	/// <summary>
	///     Disables the duration text in the middle of the small status progress bar.
	/// </summary>
	NoStatusBarText = 1L << 1,

	/// <summary>
	///     Usually only icons that are ready/usable glow, use this to ignore the button state and allow it glow whenever the
	///     status is found.
	///     Applies only when GlowBorderStatusId or GlowBorderStatusIds is defined.
	/// </summary>
	GlowIgnoresState = 1L << 2,

	/// <summary>
	///     Disables displaying big status duration when no cooldown is specified.
	/// </summary>
	NoStatusCooldownDisplay = 1L << 3
}

public class Icon : IDisposable
{
	public Bar Parent { get; }
	internal PluginLogger Logger;

	private readonly Animator _animatorBorder;
	private readonly Animator _animatorTexture;
	public IconFeatures Features = IconFeatures.Default;

	/// <summary>
	///     Action ID that will be used to lookup the icon texture.
	/// </summary>
	public uint? TextureActionId
	{
		get => _textureActionId;
		set
		{
			_textureActionId = value;
			if (value != null)
			{
				uint actionIdAdjusted = SpellHelper.GetAdjustedActionId((uint) value, TextureActionAdjust);
				_texture = SpellHelper.GetActionIconTexture(actionIdAdjusted, out bool _);
				if (_texture != null)
				{
					UpdateTextureUVs();
				}
			}
		}
	}

	private uint? _textureActionId;

	public bool TextureActionAdjust = false;

	public uint? TextureStatusId
	{
		get => _textureStatusId;
		set
		{
			_textureStatusId = value;
			if (value != null)
			{
				_texture = SpellHelper.GetStatusIconTexture((uint) value, out bool isOverriden);
				if (_texture != null)
				{
					if (!isOverriden && !Parent.Size.X.Equals(Parent.Size.Y))
					{
						// Status icons are ugly, they should always be replaced with custom ones...
						(_iconUV0, _iconUV1) = DrawHelper.GetTexCoordinates(new(_texture.Width, _texture.Height), true);
					}
					else
					{
						UpdateTextureUVs();
					}
				}
			}
		}
	}

	private uint? _textureStatusId;

	private IDalamudTextureWrap? _texture;

	/// <summary>
	///     Action ID that will be used to display cooldown spiral and text.
	/// </summary>
	public uint? CooldownActionId
	{
		get => _cooldownActionId;
		set => _cooldownActionId = value != null ? SpellHelper.GetAdjustedActionId((uint) value) : value;
	}

	private uint? _cooldownActionId;
	public float CooldownWarningThreshold = 7f;

	/// <summary>
	///     Status (NOT Action) ID for tracking the duration of a buff/debuff on a unit.
	///     By default this also shows a small progress bar unless there is no cooldown action to track.
	///     When not specifying a cooldown action the duration will be displayed like a cooldown.
	/// </summary>
	public uint? StatusId;

	public uint[]? StatusIds;
	public bool StatusSourcePlayer = true;
	public bool StatusRequired = false;

	/// <summary>
	///     Status (NOT Action) ID used for displaying stacks only.
	///     Overrides other stack counters!
	/// </summary>
	public uint? StacksStatusId;

	public Func<(byte, byte)>? CustomStacks;

	/// <summary>
	///     Action ID to lookup Status ID by name.
	/// </summary>
	public uint? StatusActionId
	{
		get => _statusActionId;
		set
		{
			_statusActionId = value;

			if (value != null)
			{
				uint actionIdAdjusted = SpellHelper.GetAdjustedActionId((uint) value);
				LuminaStatus? status = SpellHelper.GetStatusByAction(actionIdAdjusted);
				if (status != null)
				{
					StatusId = status.Value.RowId;
					Logger.Debug($"Found matching status for {value}: {StatusId}");
				}
			}
		}
	}

	private uint? _statusActionId;
	public float? MaxStatusDuration;
	public float[]? MaxStatusDurations;
	public Unit StatusTarget = Unit.Player;
	public float StatusWarningThreshold = 7f;
	public Func<(float, float)>? CustomDuration;

	public uint? GlowBorderStatusId;
	public uint[]? GlowBorderStatusIds;

	/// <summary>
	///     Additional status check when GlowBorderUsable is true AND GlowBorderInvertCheck is true AND GlowBorderStatusId(s)
	///     is set.
	///     Only show the glowing border when this status is found on the player (in addition to all the other conditions).
	/// </summary>
	public uint? GlowBorderStatusIdForced;

	/// <summary>
	///     Show glowing border if the action is usable instead of checking for a status.
	/// </summary>
	public bool GlowBorderUsable = false;

	/// <summary>
	///     Show glowing border if the status is not found/the action is not usable.
	/// </summary>
	public bool GlowBorderInvertCheck = false;

	public JobsHelper.PowerType? RequiredPowerType;
	public int? RequiredPowerAmount;
	public int? RequiredPowerAmountMax;
	public Func<bool>? CustomPowerCondition;
	public JobsHelper.PowerType? StacksPowerType;

	/// <summary>
	///     Required job level to show icon.
	/// </summary>
	public byte Level = 0;

	/// <summary>
	///     Action can only be executed while in combat.
	/// </summary>
	public bool RequiresCombat = false;

	public bool RequiresPet = false;
	public Func<bool>? CustomCondition;

	/// <summary>
	///     If the icon size ratio doesn't match the texture ratio the visible area will be cropped.
	///     By default the visible area will be the center part of your image, but you can specify clip modifiers (which are
	///     based on the image size) to move it.
	/// </summary>
	public Vector2? IconClipMultiplier
	{
		set
		{
			_clipMultiplier = value;
			if (_texture != null && _texture.ImGuiHandle != IntPtr.Zero)
			{
				Vector2 iconSize = new(_texture.Width, _texture.Height);
				(_iconUV0, _iconUV1) = DrawHelper.GetTexCoordinates(iconSize, Parent.IconSize, _clipMultiplier ?? Vector2.Zero);
			}
		}
	}

	private Vector2? _iconUV0;
	private Vector2? _iconUV1;
	private Vector2? _clipMultiplier;

	private IconState _state = IconState.Ready;

	private void UpdateTextureUVs()
	{
		IconClipMultiplier = _clipMultiplier;
	}

	public Icon(Bar parent)
	{
		Logger = new(GetType().Name);
		Parent = parent;
		_animatorBorder = new();
		_animatorBorder.Timelines.OnShow.Data.DefaultColor = Defaults.StateColors[_state].Border;
		_animatorBorder.Timelines.OnShow.Add(new ColorAnimation(_animatorBorder.Timelines.OnShow.Data.DefaultColor, _animatorBorder.Timelines.OnShow.Data.DefaultColor, 100));
		_animatorBorder.Data.Reset(_animatorBorder.Timelines.OnShow.Data);
		_animatorTexture = new();
		_animatorTexture.Timelines.OnShow.Data.DefaultColor = Defaults.StateColors[_state].Icon;
		_animatorTexture.Timelines.OnShow.Add(new ColorAnimation(_animatorTexture.Timelines.OnShow.Data.DefaultColor, _animatorTexture.Timelines.OnShow.Data.DefaultColor, 100));
		_animatorTexture.Data.Reset(_animatorTexture.Timelines.OnShow.Data);
	}

	/// <summary>
	///     Checks level, cooldown action and texture action (in this priority) to decide if this should be shown.
	/// </summary>
	/// <returns></returns>
	public bool ShouldShow()
	{
		if (Level > 0)
		{
			return Level <= (Services.ClientState.LocalPlayer?.Level ?? 0);
		}

		if (CooldownActionId != null)
		{
			return JobsHelper.IsActionUnlocked((uint) CooldownActionId);
		}

		if (TextureActionId != null)
		{
			return JobsHelper.IsActionUnlocked((uint) TextureActionId);
		}

		return true;
	}

	public void Draw(Vector2 pos, Vector2 size, Animator animator, ImDrawListPtr drawList)
	{
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
		bool failedPetCondition = RequiresPet && Services.BuddyList.PetBuddy == null;
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
			CooldownData cooldown = EventManager.Cooldown.Get((uint) CooldownActionId);

			if (cooldown.IsActive)
			{
				if (!failedInitialCondition)
				{
					newState = cooldown.Remaining / 1000f > CooldownWarningThreshold ? IconState.FadedOut : IconState.Soon;
				}

				cooldownTextRemaining = cooldown.Remaining / 1000f;
				cooldownSpiralRemaining = cooldown.Remaining / 1000f;
				cooldownSpiralTotal = cooldown.Duration / 1000f;
			}
			else if (!failedInitialCondition)
			{
				newState = IconState.Ready;
			}

			ushort maxCharges = cooldown.IsActive ? cooldown.MaxCharges : Cooldown.GetMaxCharges((uint) CooldownActionId);
			if (maxCharges > 1)
			{
				chargesTextAmount = (short) (cooldown.IsActive ? cooldown.CurrentCharges : maxCharges);
			}
		}

		// Status
		// Will be used as IconState if not checking any cooldowns.
		if ((StatusId != null || StatusIds != null) && (MaxStatusDuration != null || MaxStatusDurations != null))
		{
			bool shouldShowStatusBar = !Features.HasFlag(IconFeatures.NoStatusBar);
			bool shouldShowStatusAsCooldown = CooldownActionId == null && !Features.HasFlag(IconFeatures.NoStatusCooldownDisplay);
			Status? status = null;

			if (StatusId != null)
			{
				status = StatusTarget is Unit.TargetOrPlayer ? SpellHelper.GetStatus((uint) StatusId, Unit.Target, StatusSourcePlayer) ?? SpellHelper.GetStatus((uint) StatusId, Unit.Player, StatusSourcePlayer) : SpellHelper.GetStatus((uint) StatusId, StatusTarget, StatusSourcePlayer);
			}
			else if (StatusIds != null)
			{
				status = StatusTarget is Unit.TargetOrPlayer ? SpellHelper.GetStatus(StatusIds, Unit.Target, StatusSourcePlayer) ?? SpellHelper.GetStatus(StatusIds, Unit.Player, StatusSourcePlayer) : SpellHelper.GetStatus(StatusIds, StatusTarget, StatusSourcePlayer);
			}

			if (status != null)
			{
				// Permanent is either really permanent or a status that hasn't ticked yet.
				float duration = (MaxStatusDuration ?? 0) == Constants.PERMANENT_STATUS_DURATION ? Constants.PERMANENT_STATUS_DURATION : Math.Max(Constants.PERMANENT_STATUS_DURATION, status.RemainingTime);
				byte stacks = (duration > 0 || duration == Constants.PERMANENT_STATUS_DURATION) && status.GameData.Value.MaxStacks > 1 ? status.StackCount : (byte) 0;

				float durationMax = 0f;
				if (MaxStatusDuration != null)
				{
					durationMax = (float) MaxStatusDuration;
				}
				else if (StatusIds != null && MaxStatusDurations != null)
				{
					int index = Array.IndexOf(StatusIds, status.StatusId);
					if (index >= 0 && index < MaxStatusDurations.Length)
					{
						durationMax = MaxStatusDurations[index];
					}
				}

				// State
				if (shouldShowStatusAsCooldown && !failedInitialCondition)
				{
					newState = duration <= StatusWarningThreshold && duration >= 0 ? IconState.Soon : IconState.FadedOut;
				}

				// Progress Bar
				if (shouldShowStatusBar)
				{
					bool isPermanent = duration == Constants.PERMANENT_STATUS_DURATION || durationMax == Constants.PERMANENT_STATUS_DURATION;
					progressBarCurrent = isPermanent ? 1 : duration;
					progressBarTotal = isPermanent ? 1 : durationMax;

					// Duration Text
					if (!shouldShowStatusAsCooldown && !Features.HasFlag(IconFeatures.NoStatusBarText) && duration != Constants.PERMANENT_STATUS_DURATION)
					{
						progressBarTextRemaining = duration > 3 ? (int) Math.Ceiling(duration) : duration;
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
			Status? status = SpellHelper.GetStatus((uint) StacksStatusId, Unit.Player);
			if (status != null)
			{
				float duration = Math.Abs(status.RemainingTime);
				byte stacks = duration > 0 && status.GameData.Value.MaxStacks > 1 ? status.StackCount : (byte) 0;
				if (stacks > 0)
				{
					chargesTextAmount = stacks;
				}
			}
		}

		if (CustomDuration != null)
		{
			(float duration, float durationMax) = CustomDuration();

			bool shouldShowAsCooldown = !Features.HasFlag(IconFeatures.NoStatusCooldownDisplay) && CooldownActionId == null && ((StatusId == null && StatusIds == null) || (MaxStatusDuration == null && MaxStatusDurations == null));
			if (shouldShowAsCooldown)
			{
				// No action/status condition, only showing a custom duration. 
				cooldownTextRemaining = duration;
				cooldownSpiralRemaining = duration;
				cooldownSpiralTotal = durationMax;

				if (duration > 0)
				{
					newState = duration <= StatusWarningThreshold ? IconState.Soon : IconState.FadedOut;
				}
				else if (!failedInitialCondition)
				{
					newState = IconState.Ready;
				}
			}
			else
			{
				progressBarCurrent = duration == Constants.PERMANENT_STATUS_DURATION ? 1 : duration;
				progressBarTotal = duration == Constants.PERMANENT_STATUS_DURATION ? 1 : durationMax;

				// Duration Text
				if (duration != Constants.PERMANENT_STATUS_DURATION)
				{
					progressBarTextRemaining = duration > 3 ? (int) Math.Ceiling(duration) : duration;
				}
			}
		}

		// No conditions...
		if (!failedInitialCondition && CooldownActionId == null && StatusId == null && StatusIds == null && CustomDuration == null)
		{
			newState = IconState.Ready;
		}

		// Resources
		if (RequiredPowerType != null)
		{
			(int current, int _) = JobsHelper.GetPower((JobsHelper.PowerType) RequiredPowerType);
			if (RequiredPowerAmountMax != null)
			{
				hasEnoughResources = RequiredPowerAmount != null ? current >= RequiredPowerAmount && current <= RequiredPowerAmountMax : current <= RequiredPowerAmountMax;
			}
			else
			{
				hasEnoughResources = current >= (RequiredPowerAmount ?? 1);
			}

			if (StacksPowerType != null && current > 0)
			{
				chargesTextAmount = (short) Math.Floor((float) current);
			}
		}
		else if (CustomPowerCondition != null)
		{
			hasEnoughResources = CustomPowerCondition();
		}

		if (newState == IconState.Ready && !hasEnoughResources)
		{
			newState = IconState.ReadyOutOfResources;
		}

		if (RequiredPowerType == null && StacksPowerType != null)
		{
			(int current, int _) = JobsHelper.GetPower((JobsHelper.PowerType) StacksPowerType);
			if (current > 0)
			{
				chargesTextAmount = (short) Math.Floor((float) current);
			}
		}

		if (CustomStacks != null && chargesTextAmount == -1)
		{
			(byte current, byte _) = CustomStacks();
			if (current > 0)
			{
				chargesTextAmount = current;
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
				Status? status = SpellHelper.GetStatus((uint) GlowBorderStatusId, Unit.Player);
				float remaining = status?.RemainingTime ?? 0f;
				displayGlow = status != null && (remaining == Constants.PERMANENT_STATUS_DURATION || remaining > 0);
				if (GlowBorderInvertCheck)
				{
					displayGlow = !displayGlow;
				}
			}
			else if (GlowBorderStatusIds != null)
			{
				Status? status = SpellHelper.GetStatus(GlowBorderStatusIds, Unit.Player);
				float remaining = status?.RemainingTime ?? 0f;
				displayGlow = status != null && (remaining == Constants.PERMANENT_STATUS_DURATION || remaining > 0);
				if (GlowBorderInvertCheck)
				{
					displayGlow = !displayGlow;
				}
			}
		}
		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
		else if (GlowBorderUsable && GlowBorderInvertCheck && newState != IconState.Ready)
		{
			if (GlowBorderStatusId != null)
			{
				bool hasForcedStatus = GlowBorderStatusIdForced == null || SpellHelper.GetStatus((uint) GlowBorderStatusIdForced, Unit.Player) != null;
				if (hasForcedStatus)
				{
					Status? status = SpellHelper.GetStatus((uint) GlowBorderStatusId, Unit.Player);
					displayGlow = status == null;
				}
			}
			else if (GlowBorderStatusIds != null)
			{
				bool hasForcedStatus = GlowBorderStatusIdForced == null || SpellHelper.GetStatus((uint) GlowBorderStatusIdForced, Unit.Player) != null;
				if (hasForcedStatus)
				{
					Status? status = SpellHelper.GetStatus(GlowBorderStatusIds, Unit.Player);
					displayGlow = status == null;
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

		// TODO
		if (_textureActionId != null)
		{
			TextureActionId = _textureActionId;
		}
		else if (_textureStatusId != null)
		{
			TextureStatusId = _textureStatusId;
		}

		// Backdrop + Icon Texture
		if (newState != _state)
		{
			ColorAnimation anim1 = (ColorAnimation) _animatorBorder.Timelines.OnShow.Animations[0];
			anim1.ColorFrom = _animatorBorder.Data.Color;
			anim1.ColorTo = Defaults.StateColors[newState].Border;
			_animatorBorder.Timelines.OnShow.Data.DefaultColor = anim1.ColorFrom;
			_animatorBorder.Timelines.Loop.Data.DefaultColor = anim1.ColorTo;
			_animatorBorder.Animate();

			ColorAnimation anim2 = (ColorAnimation) _animatorTexture.Timelines.OnShow.Animations[0];
			anim2.ColorFrom = _animatorTexture.Data.Color;
			anim2.ColorTo = Defaults.StateColors[newState].Icon;
			_animatorTexture.Timelines.OnShow.Data.DefaultColor = anim2.ColorFrom;
			_animatorTexture.Timelines.Loop.Data.DefaultColor = anim2.ColorTo;
			_animatorTexture.Animate();
		}

		_animatorBorder.Update();
		_animatorTexture.Update();

		if (_texture != null && _texture.ImGuiHandle != IntPtr.Zero)
		{
			DrawHelper.DrawBackdrop(pos, size, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0.5f * animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(_animatorBorder.Data.Color.AddTransparency(animator.Data.Opacity)), drawList);
			drawList.AddImage(_texture.ImGuiHandle, posInside, posInside + sizeInside, _iconUV0 != null ? (Vector2) _iconUV0 : Parent.IconUV0, _iconUV1 != null ? (Vector2) _iconUV1 : Parent.IconUV1, ImGui.ColorConvertFloat4ToU32(_animatorTexture.Data.Color.AddTransparency(animator.Data.Opacity)));
		}
		else
		{
			DrawHelper.DrawPlaceholder("?", pos, size, animator.Data.Opacity, PlaceholderStyle.Diagonal, drawList);
		}

		// Cooldown + Charges
		if (cooldownSpiralTotal > 0)
		{
			DrawHelper.DrawProgressSwipe(posInside, sizeInside, cooldownSpiralRemaining, cooldownSpiralTotal, animator.Data.Opacity, drawList);
			using (MediaManager.PushFont(PluginFontSize.Large))
			{
				DrawHelper.DrawCooldownText(posInside, sizeInside, cooldownTextRemaining, drawList, animator.Data.Opacity);
			}
		}

		if (chargesTextAmount >= 0)
		{
			using (MediaManager.PushFont(PluginFontSize.ExtraSmall))
			{
				DrawHelper.DrawAnchoredText(TextStyle.Outline, DrawAnchor.BottomRight, chargesTextAmount.ToString(), posInside, sizeInside, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, animator.Data.Opacity)), drawList, -2, -1);
			}
		}

		// Glow
		if (displayGlow)
		{
			// Testing fake glow using images...
			// https://github.com/ocornut/imgui/issues/4706
			// https://kovart.github.io/dashed-border-generator/

			uint n = 8; // number of textures to cycle through
			float dur = 250; // duration of one full cycle
			float frameTime = dur / n; // display duration of 1 single frame
			uint step = Math.Min(n, Math.Max(1, (uint) Math.Ceiling((uint) (animator.TimeElapsed % dur) / frameTime))) - 1;
			string image = Singletons.Get<MediaManager>().BorderGlowTexture[step];

			IDalamudTextureWrap? tex = Singletons.Get<MediaManager>().GetTextureFromFilesystem(image);
			if (tex != null)
			{
				uint glowColor = ImGui.ColorConvertFloat4ToU32(new(0.95f, 0.95f, 0.32f, animator.Data.Opacity));
				drawList.AddImage(tex.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One, glowColor);
			}
			else
			{
				uint glowColor = ImGui.ColorConvertFloat4ToU32(new(206f / 255f, 1, 0, 1 * animator.Data.Opacity));
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

			DrawHelper.DrawProgressBar(progressPos, progressSize, 0, progressBarTotal, progressBarCurrent, ImGui.ColorConvertFloat4ToU32(Parent.Parent.AccentColor.AddTransparency(animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(Defaults.IconBarBGColor.AddTransparency(animator.Data.Opacity)), drawList);

			// Duration Text
			if (progressBarTextRemaining > 0)
			{
				using (MediaManager.PushFont(PluginFontSize.ExtraExtraSmall))
				{
					DrawHelper.DrawCooldownText(linePos, lineSize, progressBarTextRemaining, drawList, animator.Data.Opacity);
				}
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
		}
	}
}