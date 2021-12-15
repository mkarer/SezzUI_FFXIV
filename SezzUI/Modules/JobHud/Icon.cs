using System;
using System.Numerics;
using ImGuiNET;
using ImGuiScene;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SezzUI.Modules.JobHud
{
    public enum IconType
	{
        Default = 0
	}

    class Icon : IDisposable
    {
        /// <summary>
        /// Action ID that will be used to lookup the icon texture.
        /// </summary>
		public uint? TextureActionId
        {
            get { return _textureActionId; }
            set
            {
                if (value != null)
				{
                    LuminaAction? action = Helpers.SpellHelper.GetAction((uint)value);
                    if (action is not null)
                    {
                        _texture = DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId(action.Icon);
                    }
                }

                _textureActionId = value;
            }
        }
        private uint? _textureActionId;

		public uint? CooldownSpellId;
		public uint? AuraSpellId;
		public float? MaxDuration;

        public uint Level = 1;

        private TextureWrap? _texture;

        public Icon()
		{
		}

        public void Draw(Vector2 pos, Vector2 size, Animator.Animator animator, ImDrawListPtr drawList)
        {

            if (_texture != null)
			{
                Helpers.DrawHelper.DrawBackdrop(pos, size, drawList, animator.Data.Opacity);
                (Vector2 uv0, Vector2 uv1) = DelvUI.Helpers.DrawHelper.GetTexCoordinates(_texture, size, false);
                drawList.AddImage(_texture.ImGuiHandle, pos + Vector2.One, pos + size - Vector2.One, uv0, uv1, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, animator.Data.Opacity)));
            }
            else
			{
                Helpers.DrawHelper.DrawPlaceholder("X", pos, size, drawList, animator.Data.Opacity);
            }
        }

        public void Dispose()
        {
        }
    }
}
