using System;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;
using SezzUI.Enums;
using SezzUI.Helper;
using SezzUI.Interface;
using SezzUI.Interface.Animation;

namespace SezzUI.Modules.CooldownHud
{
	public sealed class CooldownPulse : AnimatedHudElement
	{
		public readonly long Created;

		private uint? _iconId;

		private TextureWrap? _texture;
		public uint ActionId;
		public DrawAnchor Anchor = DrawAnchor.Center;

		public byte BorderSize = 1;
		public ushort Charges = 0;

		public Vector2 IconUv0 = Vector2.Zero;
		public Vector2 IconUv1 = Vector2.One;
		public Vector2 Position = Vector2.Zero;

		public Vector2 Size = new(32f, 32f);

		public CooldownPulse()
		{
			float initialScale = 12.5f / 1.8f;
			float visibleScale = initialScale * 1.4f;
			float endScale = initialScale * 1.7f;

			Animator.Timelines.OnShow.Data.DefaultOpacity = 0;
			Animator.Timelines.OnShow.Data.DefaultScale = initialScale;
			Animator.Timelines.OnShow.Add(new FadeAnimation(0, 1, 400));
			Animator.Timelines.OnShow.Add(new ScaleAnimation(initialScale, visibleScale, 400));

			Animator.Timelines.Loop.Data.DefaultOpacity = 1;
			Animator.Timelines.Loop.Data.DefaultScale = visibleScale;

			Animator.Timelines.OnHide.Data.DefaultOpacity = 1;
			Animator.Timelines.OnHide.Data.DefaultScale = visibleScale;
			Animator.Timelines.OnHide.Add(new FadeAnimation(1, 0, 200));
			Animator.Timelines.OnHide.Add(new ScaleAnimation(visibleScale, endScale, 200));

			Created = Environment.TickCount64;
		}

		public uint? IconId
		{
			get => _iconId;
			set
			{
				_iconId = value;
				if (value != null)
				{
					Texture = Singletons.Get<TexturesCache>().GetTextureFromIconId((uint) value);
				}
			}
		}

		public TextureWrap? Texture
		{
			get => _texture;
			set
			{
				_texture = value;
				if (value != null)
				{
					float cutoff = 1.6f;
					IconUv0 = new(cutoff / value.Width, cutoff / value.Height);
					IconUv1 = new(1f - cutoff / value.Width, 1f - cutoff / value.Height);
				}
			}
		}

		public override void Draw(int elapsed = 0)
		{
			if (!IsShown && !Animator.IsAnimating)
			{
				return;
			}

			Animator.Update();

			Vector2 elementSize = Size * Animator.Data.Scale;
			Vector2 elementPosition = DrawHelper.GetAnchoredPosition(elementSize, DrawAnchor.Center) + Position + Animator.Data.Offset;

			string windowId = $"SezzUI_CooldownPulse{IconId}";
			DrawHelper.DrawInWindow(windowId, elementPosition, elementSize, false, false, drawList =>
			{
				if (_texture != null)
				{
					// Texture
					drawList.AddImage(_texture.ImGuiHandle, elementPosition, elementPosition + elementSize, IconUv0, IconUv1, ImGui.ColorConvertFloat4ToU32(Vector4.One.AddTransparency(Animator.Data.Opacity)));

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
			});

			if (!Animator.Timelines.OnShow.IsPlaying && !Animator.Timelines.OnHide.IsPlaying)
			{
				Hide();
			}
		}
	}
}