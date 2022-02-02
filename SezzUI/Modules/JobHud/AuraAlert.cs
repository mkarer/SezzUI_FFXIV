using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using ImGuiNET;
using ImGuiScene;
using SezzUI.Animator;
using SezzUI.Core;
using SezzUI.Enums;
using SezzUI.Helpers;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SezzUI.Modules.JobHud
{
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
		public Vector2 Size = Vector2.Zero;

		public DrawAnchor TextAnchor = DrawAnchor.Center;
		public Vector2 TextOffset = Vector2.Zero;

		public string? Image
		{
			get => _imagePath;
			set
			{
				_imagePath = value;
				_texture = value != null ? ImageCache.Instance.GetImageFromPath(value) : null;
			}
		}

		private string? _imagePath;
		private TextureWrap? _texture;
		public byte BorderSize = 0;

		public bool GlowBackdrop = false;
		public uint GlowBackdropSize = 8;
		public Vector4 GlowColor = Vector4.One;

		public Vector4 Color = Vector4.One;
		public Vector2 ImageUV0 = Vector2.Zero;
		public Vector2 ImageUV1 = Vector2.One;

		public bool FlipImageHorizontally
		{
			get => _flipHorizontally;
			set
			{
				if (value && !_flipHorizontally)
				{
					ImageUV0.X += 1;
					ImageUV1.X -= 1;
				}
				else if (!value && _flipHorizontally)
				{
					ImageUV0.X -= 1;
					ImageUV1.X += 1;
				}
			}
		}

		private readonly bool _flipHorizontally = false;

		public bool FlipImageVertically
		{
			get => _flipVertically;
			set
			{
				if (value && !_flipVertically)
				{
					ImageUV0.Y += 1;
					ImageUV1.Y -= 1;
				}
				else if (!value && _flipVertically)
				{
					ImageUV0.Y -= 1;
					ImageUV1.Y += 1;
				}
			}
		}

		private readonly bool _flipVertically = false;

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

		public bool UseStatusIcon(uint statusId)
		{
			LuminaStatus? status = SpellHelper.GetStatus(statusId);
			if (status != null)
			{
				_texture = TexturesCache.Instance.GetTextureFromIconId(status.Icon);
				if (_texture != null)
				{
					(ImageUV0, ImageUV1) = DrawHelper.GetTexCoordinates(Size, 0f, true); // TODO
				}
			}

			return false;
		}

		public bool UseActionIcon(uint actionId)
		{
			uint actionIdAdjusted = SpellHelper.GetAdjustedActionId(actionId);
			LuminaAction? action = SpellHelper.GetAction(actionIdAdjusted);
			if (action != null)
			{
				_texture = TexturesCache.Instance.GetTextureFromIconId(action.Icon);
				if (_texture != null)
				{
					(ImageUV0, ImageUV1) = DrawHelper.GetTexCoordinates(Size);
				}
			}

			return false;
		}

		public override void Draw(int elapsed = 0)
		{
			if (StatusId == null && StatusIds == null && PowerType == null && CustomCondition == null)
			{
				return;
			}

			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
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
				if (inCombat && !EnableInCombat || !inCombat && !EnableOutOfCombat)
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

						if (!InvertCheck && status == null || InvertCheck && status != null)
						{
							conditionsFailed = true;
							Hide(elapsed > 2000);
						}

						if (!conditionsFailed && status != null && (ExactStacks != null && status.StackCount != ExactStacks || MinimumStacks != null && status.StackCount < MinimumStacks))
						{
							conditionsFailed = true;
							Hide(elapsed > 2000);
						}
					}

					// Power Condition
					if (!conditionsFailed && PowerType != null)
					{
						(int current, int _) = JobsHelper.GetPower((JobsHelper.PowerType) PowerType);
						conditionsFailed = ExactPowerAmount != null && current != ExactPowerAmount || MinimumPowerAmount != null && current < MinimumPowerAmount;
						if (conditionsFailed)
						{
							Hide(elapsed > 2000);
						}
					}

					// Custom Condition
					if (!conditionsFailed && CustomCondition != null)
					{
						bool result = CustomCondition();
						conditionsFailed = !InvertCheck && !result || InvertCheck && result;
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

						bool fontPushed = FontsManager.Instance.PushFont("MyriadProLightCond_16");
						Vector2 textSize = ImGui.CalcTextSize(windowId);
						Vector2 textPosition = DrawHelper.GetAnchoredPosition(elementPosition, elementSize, textSize, DrawAnchor.Center);
						DrawHelper.DrawShadowText(windowId, textPosition, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, Animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, Animator.Data.Opacity)), drawList);
						if (fontPushed)
						{
							ImGui.PopFont();
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
							bool fontPushed = FontsManager.Instance.PushFont("MyriadProLightCond_20");
							Vector2 textSize = ImGui.CalcTextSize(textDuration);
							Vector2 textPosition = DrawHelper.GetAnchoredPosition(elementPosition, elementSize, textSize, TextAnchor) + TextOffset;
							DrawHelper.DrawOutlinedText(textDuration, textPosition, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 1)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 1)), drawList);
							if (fontPushed)
							{
								ImGui.PopFont();
							}
						}
					}
				});
			}
		}
	}
}