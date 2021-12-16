﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.Modules.JobHud
{
    class Bar : IDisposable
    {
        private List<Icon> _icons;
        public bool HasIcons { get { return _icons.Count > 0;  } }
    
        public Vector2 IconSize = new(38, 38); // 36px Icon + 1px Borders
        public uint IconPadding = 8;

        public Vector2 Size = Vector2.Zero;

        public Bar()
		{
            _icons = new();
		}

        public void Add(Icon icon)
		{
            if (icon.Level > 1)
			{
                PlayerCharacter? player = Service.ClientState.LocalPlayer;
                byte level = (player != null ? player.Level : (byte)0);
                if (level < icon.Level)
				{
                    return;
                }
            }

            _icons.Add(icon);

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
