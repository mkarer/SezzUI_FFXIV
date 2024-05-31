using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Internal;
using ImGuiNET;
using SezzUI.Enums;
using SezzUI.Game.Events;
using SezzUI.Helper;
using SezzUI.Interface;
using SezzUI.Interface.Animation;

namespace SezzUI.Modules.JobHud;

public sealed class AuraAlert : AnimatedHudElement
{
	public uint? StatusId;
	public uint[]? StatusIds;
	public float? MaxDuration;
	public byte? MinimumStacks;
	public byte? ExactStacks;
	public Unit StatusTarget = Unit.Player;

	public JobsHelper.PowerType? PowerType;
	public int? MinimumPowerAmount;
	public int? ExactPowerAmount;

	public Func<bool>? CustomCondition;
	public Func<float>? CustomDuration;

	public bool InvertCheck = false;
	public bool EnableInCombat = true;
	public bool EnableOutOfCombat = true;
	public bool TreatWeaponOutAsCombat = true;

	public Vector2 Position = Vector2.Zero;

	public Vector2 Size
	{
		get => _size;
		set
		{
			bool updateImageUVs = _imageFile != null && _size != value;
			_size = value;
			if (updateImageUVs)
			{
				Image = _imageFile;
			}
		}
	}

	private Vector2 _size = Vector2.Zero;

	public DrawAnchor TextAnchor = DrawAnchor.Center;
	public Vector2 TextOffset = Vector2.Zero;

	/// <summary>
	///     Image to be shown.
	///     Don't forget to set aura size first!
	/// </summary>
	public string? Image
	{
		get => _imageFile;
		set
		{
			_imageFile = value;
			_texture = value != null ? Singletons.Get<ImageCache>().GetImage(Singletons.Get<MediaManager>().GetOverlayFile(value)) : null;
			if (_texture != null)
			{
				if (_size == Vector2.Zero)
				{
					_size = new(_texture.Width, _texture.Height);
				}

				(_imageUV0, _imageUV1) = DrawHelper.GetTexCoordinates(new(_texture.Width, _texture.Height), Size);
			}
		}
	}

	private string? _imageFile;
	private IDalamudTextureWrap? _texture;
	public byte BorderSize = 0;

	public bool GlowBackdrop = false;
	public uint GlowBackdropSize = 8;
	public Vector4 GlowColor = Vector4.One;

	public Vector4 Color = Vector4.One;
	private Vector2 _imageUV0 = Vector2.Zero;
	private Vector2 _imageUV1 = Vector2.One;

	public Vector2 ImageUV0 => _imageUV0.AddXY(FlipImageHorizontally ? 1f : 0f, FlipImageVertically ? 1f : 0f);
	public Vector2 ImageUV1 => _imageUV1.AddXY(FlipImageHorizontally ? -1f : 0f, FlipImageVertically ? -1f : 0f);

	public bool FlipImageHorizontally = false;
	public bool FlipImageVertically = false;

	/// <summary>
	///     Required job level to enable alert.
	/// </summary>
	public byte Level = 1;

	private readonly float _opacityMax = 0.95f;
	private readonly float _opacityMin = 0.65f;

	public AuraAlert()
	{
		Animator.Timelines.OnShow.Data.DefaultOpacity = 0;
		Animator.Timelines.OnShow.Data.DefaultScale = 1.5f;
		Animator.Timelines.OnShow.Add(new FadeAnimation(0, _opacityMax, 250));
		Animator.Timelines.OnShow.Add(new ScaleAnimation(1.5f, 1, 250));
		Animator.Timelines.Loop.Chain(new FadeAnimation(_opacityMax, _opacityMin, 250));
		Animator.Timelines.Loop.Chain(new FadeAnimation(_opacityMin, _opacityMax, 250));
		Animator.Timelines.OnHide.Data.DefaultOpacity = 1;
		Animator.Timelines.OnHide.Add(new FadeAnimation(_opacityMax, 0, 250));
		Animator.Timelines.OnHide.Add(new ScaleAnimation(1, 1.5f, 250));
	}

	/// <summary>
	///     Uses game icon from status as texture. Can be overriden with custom images.
	///     Don't forget to set aura size first!
	/// </summary>
	/// <param name="statusId"></param>
	public void UseStatusIcon(uint statusId)
	{
		_texture = SpellHelper.GetStatusIconTexture(statusId, out bool isOverriden);
		if (_texture != null)
		{
			if (isOverriden)
			{
				(_imageUV0, _imageUV1) = DrawHelper.GetTexCoordinates(new(_texture.Width, _texture.Height), Size);
			}
			else
			{
				(_imageUV0, _imageUV1) = DrawHelper.GetTexCoordinates(new(_texture.Width, _texture.Height), true);
			}
		}
	}

	/// <summary>
	///     Uses game icon from action as texture. Can be overriden with custom images.
	///     Don't forget to set aura size first!
	/// </summary>
	/// <param name="actionId"></param>
	public void UseActionIcon(uint actionId)
	{
		uint actionIdAdjusted = SpellHelper.GetAdjustedActionId(actionId);
		_texture = SpellHelper.GetActionIconTexture(actionIdAdjusted, out bool isOverriden);
		if (_texture != null)
		{
			if (isOverriden)
			{
				(_imageUV0, _imageUV1) = DrawHelper.GetTexCoordinates(new(_texture.Width, _texture.Height), Size);
			}
			else
			{
				(_imageUV0, _imageUV1) = DrawHelper.GetTexCoordinates(new(_texture.Width, _texture.Height));
			}
		}
	}

	public override void Draw(int elapsed = 0)
	{
		if (StatusId == null && StatusIds == null && PowerType == null && CustomCondition == null)
		{
			return;
		}

		PlayerCharacter? player = Services.ClientState.LocalPlayer;
		Status? status = null;
		bool conditionsFailed = false;
		if (player == null)
		{
			conditionsFailed = true;
			Hide(elapsed > 2000);
		}
		else
		{
			bool inCombat = EventManager.Combat.IsInCombat(TreatWeaponOutAsCombat);
			if ((inCombat && !EnableInCombat) || (!inCombat && !EnableOutOfCombat))
			{
				conditionsFailed = true;
				Hide(elapsed > 2000);
			}

			if (!conditionsFailed)
			{
				// Status Condition
				if (StatusId != null || StatusIds != null)
				{
					if (StatusId != null)
					{
						status = SpellHelper.GetStatus((uint) StatusId, StatusTarget);
					}
					else if (StatusIds != null)
					{
						status = SpellHelper.GetStatus(StatusIds, StatusTarget);
					}

					if ((!InvertCheck && status == null) || (InvertCheck && status != null))
					{
						conditionsFailed = true;
						Hide(elapsed > 2000);
					}

					if (!conditionsFailed && status != null && ((ExactStacks != null && status.StackCount != ExactStacks) || (MinimumStacks != null && status.StackCount < MinimumStacks)))
					{
						conditionsFailed = true;
						Hide(elapsed > 2000);
					}
				}

				// Power Condition
				if (!conditionsFailed && PowerType != null)
				{
					(int current, int _) = JobsHelper.GetPower((JobsHelper.PowerType) PowerType);
					conditionsFailed = (ExactPowerAmount != null && current != ExactPowerAmount) || (MinimumPowerAmount != null && current < MinimumPowerAmount);
					if (conditionsFailed)
					{
						Hide(elapsed > 2000);
					}
				}

				// Custom Condition
				if (!conditionsFailed && CustomCondition != null)
				{
					bool result = CustomCondition();
					conditionsFailed = (!InvertCheck && !result) || (InvertCheck && result);
					if (conditionsFailed)
					{
						Hide(elapsed > 2000);
					}
				}
			}
		}

		if (!conditionsFailed)
		{
			Show();
		}

		if (IsShown || Animator.IsAnimating)
		{
			Animator.Update();

			// Draw aura alert
			Vector2 elementSize = Size * Animator.Data.Scale;
			Vector2 elementPosition = DrawHelper.GetAnchoredPosition(elementSize, DrawAnchor.Center) + Position + Animator.Data.Offset;

			string windowId = "SezzUI_AuraAlert";
			DrawHelper.DrawInWindow(windowId, elementPosition, elementSize, false, false, drawList =>
			{
				if (GlowBackdrop)
				{
					// Glow Backdrop
					DrawHelper.DrawBackdropEdgeGlow(elementPosition, elementSize, ImGui.ColorConvertFloat4ToU32(GlowColor.AddTransparency(Animator.Data.Opacity)), drawList, GlowBackdropSize, (short) -GlowBackdropSize);
				}

				if (_texture != null)
				{
					// Texture
					drawList.AddImage(_texture.ImGuiHandle, elementPosition, elementPosition + elementSize, ImageUV0, ImageUV1, ImGui.ColorConvertFloat4ToU32(Color.AddTransparency(Animator.Data.Opacity)));

					// Border
					if (BorderSize > 0)
					{
						drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 0.3f * Animator.Data.Opacity)), 0, ImDrawFlags.None, BorderSize);
					}
				}
				else
				{
					// Fail-over Text
					drawList.AddRectFilled(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0.5f * Animator.Data.Opacity)), 0);
					drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 0.3f * Animator.Data.Opacity)), 0, ImDrawFlags.None, 1);

					using (MediaManager.PushFont(PluginFontSize.Small))
					{
						Vector2 textSize = ImGui.CalcTextSize(windowId);
						Vector2 textPosition = DrawHelper.GetAnchoredPosition(elementPosition, elementSize, textSize, DrawAnchor.Center);
						DrawHelper.DrawShadowText(windowId, textPosition, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, Animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, Animator.Data.Opacity)), drawList);
					}
				}

				// Status Duration
				if (!InvertCheck)
				{
					float duration = CustomDuration != null ? CustomDuration() : status?.RemainingTime ?? 0;
					if (duration <= 0 && status != null && MaxDuration != null && Animator.TimeElapsed < 3000)
					{
						// Guess the duration until it is available in Dalamud?
						// Status duration seems to be 1 second longer than it should be?
						duration = (float) MaxDuration + 1 - (float) Animator.TimeElapsed / 1000;
					}

					if (duration > 0)
					{
						string textDuration = duration.ToString("0.00", Plugin.NumberFormatInfo);
						using (MediaManager.PushFont(PluginFontSize.Large))
						{
							Vector2 textSize = ImGui.CalcTextSize(textDuration);
							Vector2 textPosition = DrawHelper.GetAnchoredPosition(elementPosition, elementSize, textSize, TextAnchor) + TextOffset;
							DrawHelper.DrawOutlinedText(textDuration, textPosition, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 1)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList);
						}
					}
				}
			});
		}
	}
}