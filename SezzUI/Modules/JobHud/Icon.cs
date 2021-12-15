using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ImGuiScene;
using System.Numerics;

namespace SezzUI.Modules.JobHud
{
    class Icon : IDisposable
    {
		public uint? textureId;
        private TextureWrap? _texture;

        public Icon()
		{
		}

        public void Draw(Vector2 pos, Vector2 size, Animator.Animator animator, ImDrawListPtr drawList)
        {
            Helpers.DrawHelper.DrawPlaceholder("X", pos, size, drawList, animator.Data.Opacity);
        }

        public void Dispose()
        {
            if (_texture != null)
                _texture.Dispose();
        }
    }
}
