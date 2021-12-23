using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using System.Linq;
using SezzUI.Core;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;

namespace SezzUI.Modules.JobHud
{
	public sealed class AuraAlert : AnimatedHudElement
	{
		public uint? StatusId;
		public uint[]? StatusIds;
		public float? MaxDuration;
		public byte? MinimumStacks;
		public byte? ExactStacks;
		public Enums.Unit StatusTarget = Enums.Unit.Player;

		private uint _id = 0;
		public uint Id
		{
			get { return _id != 0 ? _id : (StatusId != null ? (uint)StatusId : _id); }
			set { _id = value; }
		}

		public bool InvertCheck = false;
		public bool EnableInCombat = true;
		public bool EnableOutOfCombat = true;
		public bool TreatWeaponOutAsCombat = true;

		public Vector2 Position = Vector2.Zero;
		public Vector2 Size = Vector2.Zero;

		public string? Image
		{
			get { return _imagePath; }
			set
			{
				_imagePath = value;
				if (value != null)
                {
                    _texture = Helpers.ImageCache.Instance.GetImageFromPath(value);
                }
                else
                {
                    _texture = null;
                }
            }
		}

		private string? _imagePath;
		private TextureWrap? _texture;

		/// <summary>
		/// Required job level to enable alert.
		/// </summary>
		public byte Level = 1;

		public AuraAlert()
		{
			Animator.Timelines.OnShow.Data.DefaultOpacity = 0;
			Animator.Timelines.OnShow.Data.DefaultScale = 1.5f;
			Animator.Timelines.OnShow.Add(new Animator.FadeAnimation(0, 0.8f, 250));
			Animator.Timelines.OnShow.Add(new Animator.ScaleAnimation(1.5f, 1, 250));
			Animator.Timelines.Loop.Chain(new Animator.FadeAnimation(0.8f, 0.6f, 250));
			Animator.Timelines.Loop.Chain(new Animator.FadeAnimation(0.6f, 0.8f, 250));
			Animator.Timelines.OnHide.Data.DefaultOpacity = 1;
			Animator.Timelines.OnHide.Add(new Animator.FadeAnimation(0.8f, 0, 250));
			Animator.Timelines.OnHide.Add(new Animator.ScaleAnimation(1, 1.5f, 250));
		}

		public override void Draw(Vector2 origin)
		{
            if (StatusId == null && StatusIds == null) { return; }

			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
			Status? status = null;
			bool conditionsFailed = false;
			if (player == null)
			{
				conditionsFailed = true;
				Hide();
			}
			else
			{
				bool inCombat = EventManager.Combat.IsInCombat(TreatWeaponOutAsCombat);
				if ((inCombat && !EnableInCombat) || (!inCombat && !EnableOutOfCombat))
				{
					conditionsFailed = true;
					Hide();
				}

				if (!conditionsFailed)
				{
					if (StatusId != null)
					{
						status = Helpers.SpellHelper.GetStatus((uint)StatusId, StatusTarget);
					}
					else if (StatusIds != null)
					{
						status = Helpers.SpellHelper.GetStatus(StatusIds, StatusTarget);

					}

					if ((!InvertCheck && status == null) || (InvertCheck && status != null))
					{
						conditionsFailed = true;
						Hide();
					}
					if (!conditionsFailed && status != null && ((ExactStacks != null && status.StackCount != ExactStacks) || (MinimumStacks != null && status.StackCount < MinimumStacks)))
					{
						conditionsFailed = true;
						Hide();
					}
				}

				if (!conditionsFailed)
				{
					Show();
				}
			}

 			if (IsShown || Animator.IsAnimating)
			{
				Animator.Update();
	
				// Draw aura alert
				Vector2 elementSize = Size * Animator.Data.Scale;
				Vector2 elementPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(origin, elementSize, Enums.DrawAnchor.Center);
				elementPosition.X += Position.X + Animator.Data.Offset.X;
				elementPosition.Y += Position.Y + Animator.Data.Offset.Y;

				string windowId = $"SezzUI_AuraAlert{Id}";
				DelvUI.Helpers.DrawHelper.DrawInWindow(windowId, elementPosition, elementSize, false, false, (drawList) =>
				{
					if (_texture != null)
					{
						// Texture
						drawList.AddImage(_texture.ImGuiHandle, elementPosition, elementPosition + elementSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, Animator.Data.Opacity)));
					}
					else
					{
						// Failover Text
						drawList.AddRectFilled(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * Animator.Data.Opacity)), 0);
						drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * Animator.Data.Opacity)), 0, ImDrawFlags.None, 1);

						bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont("MyriadProLightCond_16");
						Vector2 textSize = ImGui.CalcTextSize(windowId);
						Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementPosition + elementSize / 2, textSize, Enums.DrawAnchor.Center);
						DelvUI.Helpers.DrawHelper.DrawShadowText(windowId, textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, Animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, Animator.Data.Opacity)), drawList, 1);
						if (fontPushed) { ImGui.PopFont(); }
					}

					// Duration
					float duration = status?.RemainingTime ?? 0;
					if (duration <= 0 && status != null && MaxDuration != null && Animator.TimeElapsed < 3000)
					{
						// Guess the duration until it is available in Dalamud?
						// Status duration seems to be 1 second longer than it should be?
						duration = (float)MaxDuration + 1 - (float)Animator.TimeElapsed / 1000;
					}

					if (duration > 0)
					{
						string textDuration = duration.ToString("0.00", Plugin.NumberFormatInfo);
						bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont("MyriadProLightCond_20");
						Vector2 textSize = ImGui.CalcTextSize(textDuration);
						Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementPosition + elementSize / 2, textSize, Enums.DrawAnchor.Center);
						DelvUI.Helpers.DrawHelper.DrawOutlinedText(textDuration, textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), drawList, 1);
						if (fontPushed) { ImGui.PopFont(); }
					}
				});
			}
		}
	}
}
