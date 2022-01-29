using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;

namespace DelvUI.Helpers
{
	public static class DraggablesHelper
	{
		public static void DrawGrid(GridConfig config, HUDOptionsConfig? hudConfig, DraggableHudElement? selectedElement)
		{
			ImGui.SetNextWindowPos(Vector2.Zero);
			ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

			ImGui.SetNextWindowBgAlpha(config.BackgroundAlpha);

			ImGui.Begin("SezzUI_grid", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Vector2 screenSize = ImGui.GetMainViewport().Size;
			Vector2 offset = hudConfig != null && hudConfig.UseGlobalHudShift ? hudConfig.HudOffset : Vector2.Zero;
			Vector2 center = screenSize / 2f + offset;

			// grid
			if (config.ShowGrid)
			{
				int count = (int) (Math.Max(screenSize.X, screenSize.Y) / config.GridDivisionsDistance) / 2 + 1;

				for (int i = 0; i < count; i++)
				{
					int step = i * config.GridDivisionsDistance;

					drawList.AddLine(new(center.X + step, 0), new(center.X + step, screenSize.Y), 0x88888888);
					drawList.AddLine(new(center.X - step, 0), new(center.X - step, screenSize.Y), 0x88888888);

					drawList.AddLine(new(0, center.Y + step), new(screenSize.X, center.Y + step), 0x88888888);
					drawList.AddLine(new(0, center.Y - step), new(screenSize.X, center.Y - step), 0x88888888);

					if (config.GridSubdivisionCount > 1)
					{
						for (int j = 1; j < config.GridSubdivisionCount; j++)
						{
							int subStep = j * (config.GridDivisionsDistance / config.GridSubdivisionCount);

							drawList.AddLine(new(center.X + step + subStep, 0), new(center.X + step + subStep, screenSize.Y), 0x44888888);
							drawList.AddLine(new(center.X - step - subStep, 0), new(center.X - step - subStep, screenSize.Y), 0x44888888);

							drawList.AddLine(new(0, center.Y + step + subStep), new(screenSize.X, center.Y + step + subStep), 0x44888888);
							drawList.AddLine(new(0, center.Y - step - subStep), new(screenSize.X, center.Y - step - subStep), 0x44888888);
						}
					}
				}
			}

			// center lines
			if (config.ShowCenterLines)
			{
				drawList.AddLine(new(center.X, 0), new(center.X, screenSize.Y), 0xAAFFFFFF);
				drawList.AddLine(new(0, center.Y), new(screenSize.X, center.Y), 0xAAFFFFFF);
			}

			if (config.ShowAnchorPoints && selectedElement != null)
			{
				Vector2 parentAnchorPos = center + selectedElement.ParentPos();
				Vector2 anchorPos = parentAnchorPos + selectedElement.GetConfig().Position;

				drawList.AddLine(parentAnchorPos, anchorPos, 0xAA0000FF, 2);

				Vector2 anchorSize = new(10, 10);
				drawList.AddRectFilled(anchorPos - anchorSize / 2f, anchorPos + anchorSize / 2f, 0xAA0000FF);
			}

			ImGui.End();
		}

		public static void DrawElements(Vector2 origin, HudHelper hudHelper, List<DraggableHudElement> elements, DraggableHudElement? selectedElement)
		{
			bool canTakeInput = true;

			// selected
			if (selectedElement != null)
			{
				if (!hudHelper.IsElementHidden(selectedElement))
				{
					selectedElement.CanTakeInputForDrag = true;
					selectedElement.Draw(origin);
					canTakeInput = !selectedElement.NeedsInputForDrag;
				}
				else if (selectedElement is IHudElementWithMouseOver elementWithMouseOver)
				{
					elementWithMouseOver.StopMouseover();
				}
			}

			// all
			foreach (DraggableHudElement element in elements)
			{
				if (element == selectedElement)
				{
					continue;
				}

				if (!hudHelper.IsElementHidden(element))
				{
					element.CanTakeInputForDrag = canTakeInput;
					element.Draw(origin);
					canTakeInput = !canTakeInput ? false : !element.NeedsInputForDrag;
				}
				else if (element is IHudElementWithMouseOver elementWithMouseOver)
				{
					elementWithMouseOver.StopMouseover();
				}
			}
		}

		public static bool DrawArrows(Vector2 position, Vector2 size, string tooltipText, out Vector2 offset)
		{
			offset = Vector2.Zero;

			ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoSavedSettings;

			Vector2 margin = new(4, 0);
			Vector2 windowSize = ArrowSize + margin * 2;

			// left, right, up, down
			Vector2[] positions = GetArrowPositions(position, size);
			Vector2[] offsets = new[]
			{
				new(-1, 0),
				new Vector2(1, 0),
				new Vector2(0, -1),
				new Vector2(0, 1)
			};

			for (int i = 0; i < 4; i++)
			{
				Vector2 pos = positions[i] - margin;

				ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
				ImGui.SetNextWindowPos(pos);

				ImGui.Begin("SezzUI_draggablesArrow " + i, windowFlags);

				// fake button
				ImGui.ArrowButton("arrow button " + i, (ImGuiDir) i);

				if (ImGui.IsMouseHoveringRect(pos, pos + windowSize))
				{
					// track click manually to not deal with window focus stuff
					if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					{
						offset = offsets[i];
					}

					// tooltip
					TooltipsHelper.Instance.ShowTooltipOnCursor(tooltipText);
				}

				ImGui.End();
			}

			return offset != Vector2.Zero;
		}

		public static Vector2 ArrowSize = new(40, 40);

		public static Vector2[] GetArrowPositions(Vector2 position, Vector2 size) => GetArrowPositions(position, size, ArrowSize);

		public static Vector2[] GetArrowPositions(Vector2 position, Vector2 size, Vector2 arrowSize)
		{
			return new[]
			{
				new(position.X - arrowSize.X + 10, position.Y + size.Y / 2f - arrowSize.Y / 2f - 2),
				new Vector2(position.X + size.X - 8, position.Y + size.Y / 2f - arrowSize.Y / 2f - 2),
				new Vector2(position.X + size.X / 2f - arrowSize.X / 2f + 2, position.Y - arrowSize.Y + 1),
				new Vector2(position.X + size.X / 2f - arrowSize.X / 2f + 2, position.Y + size.Y - 7)
			};
		}
	}
}