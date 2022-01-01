using System;
using System.Collections.Generic;
using SezzUI.Interface.GeneralElements;
using System.Numerics;
using ImGuiNET;
using SezzUI.Interface;
using SezzUI.Enums;

namespace SezzUI.Modules.GameUI
{
    public class InteractableArea : ParentAnchoredDraggableHudElement
    {
        private InteractableAreaConfig Config => (InteractableAreaConfig)_config;
     
        public bool Enabled = true;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = Vector2.Zero;
        public DrawAnchor Anchor = DrawAnchor.Center;
        public List<Element> Elements = new();
        public bool IsHovered = false;
        public bool DrawPlaceholder = false;

        public void Draw()
        {
            Vector2 pos = DelvUI.Helpers.Utils.GetAnchoredPosition(Position, Size, Anchor);
            IsHovered = ImGui.IsMouseHoveringRect(pos, pos + Size); // TODO: Check if window is active?

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

        public InteractableArea(InteractableAreaConfig config) : base(config)
        {
        }
    }
}
