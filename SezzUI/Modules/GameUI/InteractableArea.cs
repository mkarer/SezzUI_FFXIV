using System.Numerics;
using ImGuiNET;
using SezzUI.Helpers;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Modules.GameUI
{
	public class InteractableArea : DraggableHudElement
	{
		public bool IsHovered;

		public InteractableAreaConfig Config => (InteractableAreaConfig) _config;

		public InteractableArea(InteractableAreaConfig config) : base(config)
		{
		}

		public override void DrawChildren(Vector2 origin)
		{
			Vector2 anchoredPosition = DrawHelper.GetAnchoredPosition(Config.Size, Config.Anchor) + Config.Position;
			IsHovered = ImGui.IsMouseHoveringRect(anchoredPosition, anchoredPosition + Config.Size);
		}
	}
}