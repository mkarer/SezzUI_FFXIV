using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SezzUI.Core;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;
using Dalamud.Logging;

namespace SezzUI.Modules.JobHud
{
	class AuraAlert : AnimatedHudElement
	{
		public uint StatusId = 2594; // Soulsow (Able to execute Harvest Moon.)
		public bool InvertCheck = true; // Show when buff is missing!
		public bool EnableInCombat = false;
		public bool EnableOutOfCombat = true;
		public bool TreatWeaponOutAsCombat = false;

		public string Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\surge_of_darkness.png";
		private TextureWrap? _imageTexture;

		public AuraAlert()
		{
			_imageTexture = Plugin.PluginInterface.UiBuilder.LoadImage(Image);

			Animator.Timelines.OnShow.Data.DefaultOpacity = 0;
			Animator.Timelines.OnShow.Data.DefaultScale = 1.5f;
			Animator.Timelines.OnShow.Add(new Animator.FadeAnimation(0, 0.8f, 250));
			Animator.Timelines.OnShow.Add(new Animator.ScaleAnimation(1.5f, 1, 250));

			Animator.Timelines.Loop.Add(new Animator.FadeAnimation(0.8f, 0.6f, 250, 0));
			Animator.Timelines.Loop.Add(new Animator.FadeAnimation(0.6f, 0.8f, 250, 250));

			Animator.Timelines.OnHide.Data.DefaultOpacity = 0f;
			Animator.Timelines.OnHide.Add(new Animator.FadeAnimation(0.8f, 0, 250));
			Animator.Timelines.OnHide.Add(new Animator.ScaleAnimation(1, 1.5f, 250));
		}

		public override void Draw(Vector2 origin)
		{
			PlayerCharacter? player = Service.ClientState.LocalPlayer;
			bool conditionsFailed = false;
			if (player == null)
			{
				conditionsFailed = true;
				Hide();
			}
			else
			{
				bool inCombat = Plugin.SezzUIPlugin.Events.Combat.IsInCombat(TreatWeaponOutAsCombat);
				if ((inCombat && !EnableInCombat) || (!inCombat && !EnableOutOfCombat))
				{
					conditionsFailed = true;
					Hide();
				}

				if (!conditionsFailed)
				{
					bool hasAura = (player.StatusList.FirstOrDefault(o => o.StatusId == StatusId) != null);
					if ((!InvertCheck && !hasAura) || (InvertCheck && hasAura))
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
				Vector2 elementSize = new Vector2(128, 256) * Animator.Data.Scale;
				Vector2 elementPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(origin, elementSize, DelvUI.Enums.DrawAnchor.Center);
				elementPosition.X += -140 + Animator.Data.X;
				elementPosition.Y += 50 + Animator.Data.Y;

				DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_AuraAlert1", elementPosition, elementSize, false, false, (drawList) =>
				{
					if (_imageTexture != null)
					{
						// Texture
						drawList.AddImage(_imageTexture.ImGuiHandle, elementPosition, elementPosition + elementSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, Animator.Data.Opacity)));
					}
					else
					{
						// Failover Text
						drawList.AddRectFilled(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * Animator.Data.Opacity)), 0);
						drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * Animator.Data.Opacity)), 0, ImDrawFlags.None, 1);

						bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont("MyriadProLightCond_16");
						string text = "SezzUI_AuraAlert1";
						Vector2 textSize = ImGui.CalcTextSize(text);
						Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementPosition + elementSize / 2, textSize, DelvUI.Enums.DrawAnchor.Center);
						DelvUI.Helpers.DrawHelper.DrawShadowText(text, textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, Animator.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, Animator.Data.Opacity)), drawList, 1);

						if (fontPushed) { ImGui.PopFont(); }
					}
				});
			}
		}
	}
}
