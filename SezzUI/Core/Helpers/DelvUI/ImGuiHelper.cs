using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Config.Tree;
using SezzUI.Enums;

namespace DelvUI.Helpers
{
	public static class ImGuiHelper
	{
		public static void PushButtonStyle(float opacity = 1f, Vector2? padding = null)
		{
			PushButtonStyle(1f, opacity, padding);
		}

		private static uint _buttonColor = ImGui.ColorConvertFloat4ToU32(new(0f, 0f, 0f, 0.5f));
		private static uint _buttonColorHovered = ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.15f));
		private static uint _buttonColorActive = ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.25f));
		private static uint _buttonColorBorder = ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 77f / 255f));

		public static void PushButtonStyle(float borderSize, float opacity = 1f, Vector2? padding = null)
		{
			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding ?? new(borderSize, borderSize));
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, borderSize);
			ImGui.PushStyleColor(ImGuiCol.Button, _buttonColor);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, _buttonColorHovered);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, _buttonColorActive);
			ImGui.PushStyleColor(ImGuiCol.Border, _buttonColorBorder);
		}

		public static void PopButtonStyle()
		{
			ImGui.PopStyleVar(4);
			ImGui.PopStyleColor(4);
		}

		public static bool FontAwesomeIconButton(string label, FontAwesomeIcon icon, Vector2 size, string? id = null)
		{
			Vector2 buttonPosition = ImGui.GetCursorScreenPos();
			Vector2 buttonSize = new(size.X, ImGui.GetFrameHeight());
			string buttonId = id ?? label;
			bool clicked = ImGui.Button("##SezzUI_IconButton" + buttonId, size); // Fake button

			// ImGui.SetCursorPos(textPosition.AddY((contentSize.Y - iconSize.Y) / 2f));
			// ImGui.Text(iconString);
			// ImGui.PopFont();
			// ImGui.SetCursorPos(textPosition + new Vector2(iconSize.X + 6, (contentSize.Y - textSize.Y) / 2f - 1));
			// ImGui.Text(label);

			// Looks like we have to draw the labels in new window,
			// otherwise ImGui.SameLine() doesn't work correctly anymore?!
			DrawHelper.DrawInWindow("##SezzUI_IconButtonContent" + buttonId, buttonPosition, buttonSize, false, false, drawList =>
			{
				// Content
				string iconString = icon.ToIconString();
				Vector2 textSize = ImGui.CalcTextSize(label);

				ImGui.PushFont(UiBuilder.IconFont);
				Vector2 iconSize = ImGui.CalcTextSize(iconString);
				Vector2 contentSize = new(textSize.X + iconSize.X + 6, Math.Max(textSize.Y, iconSize.Y));

				Vector2 textPosition = SezzUI.Helpers.DrawHelper.GetAnchoredPosition(buttonPosition, buttonSize, contentSize, DrawAnchor.Center);
				drawList.AddText(textPosition.AddY((contentSize.Y - iconSize.Y) / 2f), 0xffffffff, iconString);
				ImGui.PopFont();
				drawList.AddText(textPosition + new Vector2(iconSize.X + 6, (contentSize.Y - textSize.Y) / 2f - 1), 0xffffffff, label);
			});

			return clicked;
		}

		public static void DrawSeparator(int topSpacing, int bottomSpacing)
		{
			DrawSpacing(topSpacing);
			ImGui.Separator();
			DrawSpacing(bottomSpacing);
		}

		public static void DrawSpacing(int spacingSize)
		{
			for (int i = 0; i < spacingSize; i++)
			{
				ImGui.NewLine();
			}
		}

		public static void NewLineAndTab()
		{
			ImGui.NewLine();
			Tab();
		}

		public static void Tab()
		{
			ImGui.Text("\u2002");
			ImGui.SameLine();
		}

		public static Node? DrawExportResetContextMenu(Node node, bool canExport, bool canReset)
		{
			Node? nodeToReset = null;

			if (ImGui.BeginPopupContextItem())
			{
				if (canExport && ImGui.Selectable("Export"))
				{
					string? exportString = node.GetBase64String();
					ImGui.SetClipboardText(exportString ?? "");
				}

				if (canReset && ImGui.Selectable("Reset"))
				{
					ImGui.CloseCurrentPopup();
					nodeToReset = node;
				}

				ImGui.EndPopup();
			}

			return nodeToReset;
		}

		public static (bool, bool) DrawConfirmationModal(string title, string message)
		{
			return DrawConfirmationModal(title, new[] {message});
		}

		public static (bool, bool) DrawConfirmationModal(string title, IEnumerable<string> textLines)
		{
			ConfigurationManager.Instance.ShowingModalWindow = true;

			bool didConfirm = false;
			bool didClose = false;

			ImGui.OpenPopup(title + " ##SezzUI");

			Vector2 center = ImGui.GetMainViewport().GetCenter();
			ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

			bool p_open = true; // i've no idea what this is used for

			if (ImGui.BeginPopupModal(title + " ##SezzUI", ref p_open, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
			{
				float width = 300;
				float height = Math.Min((ImGui.CalcTextSize(" ").Y + 5) * textLines.Count(), 240);

				ImGui.BeginChild("confirmation_modal_message", new(width, height), false);
				foreach (string text in textLines)
				{
					ImGui.Text(text);
				}

				ImGui.EndChild();

				ImGui.NewLine();

				if (ImGui.Button("OK", new(width / 2f - 5, 24)))
				{
					ImGui.CloseCurrentPopup();
					didConfirm = true;
					didClose = true;
				}

				ImGui.SetItemDefaultFocus();
				ImGui.SameLine();
				if (ImGui.Button("Cancel", new(width / 2f - 5, 24)))
				{
					ImGui.CloseCurrentPopup();
					didClose = true;
				}

				ImGui.EndPopup();
			}
			// close button on nav
			else
			{
				didClose = true;
			}

			if (didClose)
			{
				ConfigurationManager.Instance.ShowingModalWindow = false;
			}

			return (didConfirm, didClose);
		}

		public static bool DrawErrorModal(string message)
		{
			ConfigurationManager.Instance.ShowingModalWindow = true;

			bool didClose = false;
			ImGui.OpenPopup("Error ##SezzUI");

			Vector2 center = ImGui.GetMainViewport().GetCenter();
			ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

			bool p_open = true; // i've no idea what this is used for
			if (ImGui.BeginPopupModal("Error ##SezzUI", ref p_open, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
			{
				ImGui.Text(message);
				ImGui.NewLine();

				float textSize = ImGui.CalcTextSize(message).X;

				if (ImGui.Button("OK", new(textSize, 24)))
				{
					ImGui.CloseCurrentPopup();
					didClose = true;
				}

				ImGui.EndPopup();
			}
			// close button on nav
			else
			{
				didClose = true;
			}

			if (didClose)
			{
				ConfigurationManager.Instance.ShowingModalWindow = false;
			}

			return didClose;
		}

		public static (bool, bool) DrawInputModal(string title, string message, ref string value)
		{
			ConfigurationManager.Instance.ShowingModalWindow = true;

			bool didConfirm = false;
			bool didClose = false;

			ImGui.OpenPopup(title + " ##SezzUI");

			Vector2 center = ImGui.GetMainViewport().GetCenter();
			ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

			bool p_open = true; // i've no idea what this is used for

			if (ImGui.BeginPopupModal(title + " ##SezzUI", ref p_open, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
			{
				float textSize = ImGui.CalcTextSize(message).X;

				ImGui.Text(message);

				ImGui.PushItemWidth(textSize);
				ImGui.InputText("", ref value, 64);

				ImGui.NewLine();
				if (ImGui.Button("OK", new(textSize / 2f - 5, 24)))
				{
					ImGui.CloseCurrentPopup();
					didConfirm = true;
					didClose = true;
				}

				ImGui.SetItemDefaultFocus();
				ImGui.SameLine();
				if (ImGui.Button("Cancel", new(textSize / 2f - 5, 24)))
				{
					ImGui.CloseCurrentPopup();
					didClose = true;
				}

				ImGui.EndPopup();
			}
			// close button on nav
			else
			{
				didClose = true;
			}

			if (didClose)
			{
				ConfigurationManager.Instance.ShowingModalWindow = false;
			}

			return (didConfirm, didClose);
		}
	}
}