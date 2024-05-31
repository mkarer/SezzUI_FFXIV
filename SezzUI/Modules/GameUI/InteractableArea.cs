using System.Numerics;
using ImGuiNET;
using SezzUI.Helper;
using SezzUI.Interface;

namespace SezzUI.Modules.GameUI;

public class InteractableArea : DraggableHudElement
{
	public bool IsHovered;
	public new InteractableAreaConfig Config => (InteractableAreaConfig) _config;

	public override string? DisplayName => Config.Description;

	public InteractableArea(InteractableAreaConfig config) : base(config)
	{
	}

	public void Draw()
	{
		Vector2 anchoredPosition = DrawHelper.GetAnchoredPosition(_config.Size, _config.Anchor) + _config.Position;
		IsHovered = ImGui.IsMouseHoveringRect(anchoredPosition, anchoredPosition + _config.Size);
	}
}