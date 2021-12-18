using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.Modules.JobHud
{
    public sealed class Bar : IDisposable
    {
        private List<Icon> _icons;
        public bool HasIcons { get { return _icons.Count > 0; } }
    
        public Vector2 IconSize
        {
            get { return _iconSize; }
            set
			{
                _iconSize = value;

                IconUV0 = new(1f / IconSize.X, 1f / IconSize.Y);
                IconUV1 = new(1f - 1f / IconSize.X, 1f - 1f / IconSize.Y);

                if (IconSize.X != IconSize.Y)
                {
                    float ratio = Math.Max(IconSize.X, IconSize.Y) / Math.Min(IconSize.X, IconSize.Y);
                    float crop = (1 - (1 / ratio)) / 2;

                    if (IconSize.X < IconSize.Y)
                    {
                        // Crop left/right parts
                        IconUV0.X += crop;
                        IconUV1.X -= crop;
                    }
                    else
                    {
                        // Crop top/bottom parts
                        IconUV0.Y += crop;
                        IconUV1.Y -= crop;
                    }
                }
            }
        }
        public Vector2 IconUV0 = new Vector2(0, 0);
        public Vector2 IconUV1 = new Vector2(1, 1);
        public uint IconPadding = 8;
        private Vector2 _iconSize; // 36px Icon + 1px Borders

        public Vector2 Size = Vector2.Zero;

        public Bar()
		{
            _icons = new();
            IconSize = new(38, 38); // 36px Icon + 1px Borders
        }

        public void Add(Icon icon, int index = -1)
		{
            if (icon.Level > 1 && (Service.ClientState.LocalPlayer?.Level ?? 0) < icon.Level) return;

            if (index == -1)
			{
                _icons.Add(icon);
            }
            else
			{
                _icons.Insert(index, icon);
            }

            Size.Y = IconSize.Y;
            Size.X = IconSize.X * _icons.Count() + (_icons.Count() - 1) * IconPadding;
        }

        public void Dispose()
        {
            _icons.ForEach(i => i.Dispose());
        }

        public void Draw(Vector2 origin, Animator.Animator animator)
        {
            if (!HasIcons) return;

            Vector2 pos = DelvUI.Helpers.Utils.GetAnchoredPosition(origin, Size, DelvUI.Enums.DrawAnchor.Top);

            DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_JobHudBar", pos, Size, false, false, (drawList) => {
                Vector2 iconPos = Vector2.Zero;
                iconPos.Y = pos.Y;

                //Helpers.DrawHelper.DrawPlaceholder("Bar", pos, Size, drawList, animator.Data.Opacity);

                for (int i = 0; i < _icons.Count; i++)
                {
                    iconPos.X = pos.X + i * (IconPadding + IconSize.X);
                    _icons[i].Draw(iconPos, IconSize, animator, drawList);
                }
            });
        }
    }
}
