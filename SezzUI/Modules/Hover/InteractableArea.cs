using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using SezzUI.Enums;

namespace SezzUI.Modules.Hover
{
    public class InteractableArea : IDisposable
    {
        public bool Enabled = true;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = Vector2.Zero;
        public DrawAnchor Anchor = DrawAnchor.Center;
        public List<String> Elements = new();
        public string ID = $"SezzUI_InteractableArea_{Guid.NewGuid()}";
        public bool IsHovered = false;
        public bool DrawPlaceholder = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Draw()
        {
            Vector2 pos = DelvUI.Helpers.Utils.GetAnchoredPosition(Position, Size, Anchor);
            IsHovered = ImGui.IsMouseHoveringRect(pos, pos + Size);

            if (DrawPlaceholder)
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                ImGui.SetNextWindowPos(pos);
                ImGui.SetNextWindowSize(Size);

                var begin = ImGui.Begin(
                   ID,
                    ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.NoBackground
                    | ImGuiWindowFlags.NoInputs
                    | ImGuiWindowFlags.NoMove
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoFocusOnAppearing
                    | ImGuiWindowFlags.NoSavedSettings
                );

                if (!begin)
                {
                    ImGui.End();
                    return;
                }

                Helpers.DrawHelper.DrawPlaceholder(IsHovered ? "YO" : "NAH", pos, Size, 1, drawList);

                ImGui.End();
            }
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
        }
    }
}
