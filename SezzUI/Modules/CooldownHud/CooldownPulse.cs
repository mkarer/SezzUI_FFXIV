using System;
using SezzUI.Core;
using SezzUI.Enums;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;

namespace SezzUI.Modules.CooldownHud
{
    public sealed class CooldownPulse : AnimatedHudElement
    {
        public uint ActionId;
        public ushort Charges = 0;
        public long Created;

        public uint? IconId
        {
            get { return _iconId; }
            set
            {
                _iconId = value;
                if (value != null)
                {
                    Texture = DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId((uint)value);
                }
            }
        }
        private uint? _iconId;

        public TextureWrap? Texture
        {
            get { return _texture; }
            set
            {
                _texture = value;
                if (value != null)
                {
                    IconUV0 = new(1f / value.Width, 1f / value.Height);
                    IconUV1 = new(1f - 1f / value.Width, 1f - 1f / value.Height);
                }
            }
        }
        private TextureWrap? _texture;

        public Vector2 IconUV0 = Vector2.Zero;
        public Vector2 IconUV1 = Vector2.One;

        public Vector2 Size = new(32f, 32f);
        public Vector2 Position = Vector2.Zero;
        public DrawAnchor Anchor = DrawAnchor.Center;

        public byte BorderSize = 1;

        public CooldownPulse()
        {
            float initialScale = 12.5f / 1.8f;
            float visibleScale = initialScale * 1.4f;
            float endScale = initialScale * 1.7f;

            Animator.Timelines.OnShow.Data.DefaultOpacity = 0;
            Animator.Timelines.OnShow.Data.DefaultScale = initialScale;
            Animator.Timelines.OnShow.Add(new Animator.FadeAnimation(0, 1, 400));
            Animator.Timelines.OnShow.Add(new Animator.ScaleAnimation(initialScale, visibleScale, 400));

            Animator.Timelines.Loop.Data.DefaultOpacity = 1;
            Animator.Timelines.Loop.Data.DefaultScale = visibleScale;

            Animator.Timelines.OnHide.Data.DefaultOpacity = 1;
            Animator.Timelines.OnHide.Data.DefaultScale = visibleScale;
            Animator.Timelines.OnHide.Add(new Animator.FadeAnimation(1, 0, 200));
            Animator.Timelines.OnHide.Add(new Animator.ScaleAnimation(visibleScale, endScale, 200));

            Created = Environment.TickCount64;
        }

        public override void Draw(Vector2 origin, int elapsed = 0)
        {
            if (!IsShown && !Animator.IsAnimating) { return; }

            Animator.Update();

            Vector2 elementSize = Size * Animator.Data.Scale;
            Vector2 elementPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(origin, elementSize, Enums.DrawAnchor.Center);
            elementPosition.X += Position.X + Animator.Data.Offset.X;
            elementPosition.Y += Position.Y + Animator.Data.Offset.Y;

            string windowId = $"SezzUI_CooldownPulse{IconId}";
            DelvUI.Helpers.DrawHelper.DrawInWindow(windowId, elementPosition, elementSize, false, false, (drawList) =>
            {
                if (_texture != null)
                {
                    // Texture
                    drawList.AddImage(_texture.ImGuiHandle, elementPosition, elementPosition + elementSize, IconUV0, IconUV1, ImGui.ColorConvertFloat4ToU32(Vector4.One.AddTransparency(Animator.Data.Opacity)));

                    // Border
                    if (BorderSize > 0)
                    {
                        drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * Animator.Data.Opacity)), 0, ImDrawFlags.None, BorderSize);
                    }
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
            });

            if (!Animator.Timelines.OnShow.IsPlaying && !Animator.Timelines.OnHide.IsPlaying)
            {
                Hide();
            }
        }
    }
}
