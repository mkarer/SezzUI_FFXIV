using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace SezzUI.Modules.JobHud
{
    class Bar : IDisposable
    {
        private List<Icon> _icons;
        public Vector2 IconSize = new(36, 36);
        public uint IconPadding = 4;
        public Vector2 Size = Vector2.Zero;

        public Bar()
		{
            _icons = new();
		}

        public void Add(Icon icon)
		{
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
