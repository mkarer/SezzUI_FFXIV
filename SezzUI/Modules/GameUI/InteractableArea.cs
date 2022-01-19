using System.Collections.Generic;
using System.Numerics;
using DelvUI.Helpers;
using ImGuiNET;
using SezzUI.Enums;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;
using DrawHelper = SezzUI.Helpers.DrawHelper;

namespace SezzUI.Modules.GameUI
{
	public class InteractableArea : ParentAnchoredDraggableHudElement
	{
		public DrawAnchor Anchor = DrawAnchor.Center;
		public bool DrawPlaceholder = false;
		public List<Element> Elements = new();

		public bool Enabled = true;
		public bool IsHovered;
		public Vector2 Position = Vector2.Zero;
		public Vector2 Size = Vector2.Zero;

		public InteractableArea(InteractableAreaConfig config) : base(config)
		{
		}

		private InteractableAreaConfig Config => (InteractableAreaConfig) _config;

		public void Draw()
		{
			Vector2 pos = Utils.GetAnchoredPosition(Position, Size, Anchor);
			IsHovered = ImGui.IsMouseHoveringRect(pos, pos + Size); // TODO: Check if window is active?

			if (DrawPlaceholder)
			{
				ImDrawListPtr drawList = ImGui.GetWindowDrawList();

				ImGui.SetNextWindowPos(pos);
				ImGui.SetNextWindowSize(Size);

				bool begin = ImGui.Begin(ID, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings);

				if (!begin)
				{
					ImGui.End();
					return;
				}

				DrawHelper.DrawPlaceholder(IsHovered ? "YO" : "NAH", pos, Size, 1, drawList);

				ImGui.End();
			}
		}
	}
}