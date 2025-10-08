using System;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using SezzUI.Configuration;
using SezzUI.Configuration.Tree;
using SezzUI.Enums;
using SezzUI.Interface;
using SezzUI.Logging;

namespace SezzUI.Helper;

public static class ImGuiHelper
{
	internal static PluginLogger Logger;

	static ImGuiHelper()
	{
		Logger = new("ImGuiHelper");
	}

	public static void PushButtonStyle(float opacity = 1f, Vector2? padding = null)
	{
		PushButtonStyle(1f, opacity, padding);
	}

	private static readonly uint _buttonColor = ImGui.ColorConvertFloat4ToU32(new(0f, 0f, 0f, 0.5f));
	private static readonly uint _buttonColorHovered = ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.15f));
	private static readonly uint _buttonColorActive = ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.25f));
	private static readonly uint _buttonColorBorder = ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 77f / 255f));

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

			Vector2 textPosition = DrawHelper.GetAnchoredPosition(buttonPosition, buttonSize, contentSize, DrawAnchor.Center);
			drawList.AddText(textPosition.AddY((contentSize.Y - iconSize.Y) / 2f), 0xffffffff, iconString);
			ImGui.PopFont();
			drawList.AddText(textPosition + new Vector2(iconSize.X + 6, (contentSize.Y - textSize.Y) / 2f - 1), 0xffffffff, label);
		});

		return clicked;
	}

	#region File/Folder Dialogs

	public static void SelectFolder(FileDialogManager fileDialogManager, string title, Action<bool, string> callback, string? selected = null)
	{
		Action<bool, string> validatedCallback = (finished, path) =>
		{
			if (finished && path.Length > 0 && FileSystemHelper.ValidatePath(path, out string validatedPath))
			{
				path = validatedPath;
			}

			callback(finished, path);
		};

		fileDialogManager.OpenFolderDialog(title, validatedCallback);

		if (FileSystemHelper.ValidatePath(selected, out string validatedSelectedPath))
		{
			try
			{
				FileDialog fileDialog = fileDialogManager.GetFieldValue<FileDialog>("dialog");
				fileDialog.GetType().GetTypeInfo().GetDeclaredMethod("SetPath")?.Invoke(fileDialog, new object[] {validatedSelectedPath});
			}
			catch (Exception ex)
			{
				Logger.Error($"Error setting FileDialogManager path: {ex}");
			}
		}
	}

	#endregion

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

		if (ImGui.BeginPopupContextItem("ResetContextMenu"))
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

	public static (bool, bool) DrawConfirmationModal(string title, string[] textLines)
	{
		if (textLines.Length == 0)
		{
			return (false, true);
		}

		Singletons.Get<ConfigurationManager>().ShowingModalWindow = true;

		bool didConfirm = false;
		bool didClose = false;

		ImGui.OpenPopup(title + " ##SezzUI");

		Vector2 center = ImGui.GetMainViewport().GetCenter();
		ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

		bool pOpen = true; // i've no idea what this is used for

		if (ImGui.BeginPopupModal(title + " ##SezzUI", ref pOpen, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
		{
			float height = Math.Min((ImGui.CalcTextSize(" ").Y + 5) * textLines.Length, 240);

			float width = 300;
			foreach (string textLine in textLines)
			{
				width = Math.Max(width, ImGui.CalcTextSize(textLine).X);
			}

			ImGui.BeginChild("confirmation_modal_message", new(width, height));
			foreach (string text in textLines)
			{
				if (text.StartsWith("\u24d8"))
				{
					ImGui.TextColored(Style.Colors.Hint.Vector, text[1..]);
				}
				else
				{
					ImGui.Text(text);
				}
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
			Singletons.Get<ConfigurationManager>().ShowingModalWindow = false;
		}

		return (didConfirm, didClose);
	}

	public static bool DrawErrorModal(string message)
	{
		Singletons.Get<ConfigurationManager>().ShowingModalWindow = true;

		bool didClose = false;
		ImGui.OpenPopup("Error ##SezzUI");

		Vector2 center = ImGui.GetMainViewport().GetCenter();
		ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

		bool pOpen = true; // i've no idea what this is used for
		if (ImGui.BeginPopupModal("Error ##SezzUI", ref pOpen, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
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
			Singletons.Get<ConfigurationManager>().ShowingModalWindow = false;
		}

		return didClose;
	}

	public static (bool, bool) DrawInputModal(string title, string message, ref string value)
	{
		Singletons.Get<ConfigurationManager>().ShowingModalWindow = true;

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
			Singletons.Get<ConfigurationManager>().ShowingModalWindow = false;
		}

		return (didConfirm, didClose);
	}

	public static bool Button(string label, FontAwesomeIcon icon, string? help = null, Vector2? size = null)
	{
		if (!string.IsNullOrEmpty(label))
		{
			ImGui.Text(label);
			ImGui.SameLine();
		}

		ImGui.PushFont(UiBuilder.IconFont);
		bool clicked = ImGui.Button(icon.ToIconString(), size ?? Vector2.Zero);

		ImGui.PopFont();
		if (!string.IsNullOrEmpty(help) && ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(help);
		}

		return clicked;
	}

	#region Configuration

	public static void DrawNestIndicator(int depth)
	{
		// This draws the L shaped symbols and padding to the left of config items collapsible under a checkbox.
		// Shift cursor to the right to pad for children with depth more than 1.
		// 26 is an arbitrary value I found to be around half the width of a checkbox
		Vector2 oldCursor = ImGui.GetCursorPos();
		Vector2 offset = new(26 * Math.Max(depth - 1, 0), 2);
		ImGui.SetCursorPos(oldCursor + offset);
		ImGui.TextColored(ImGui.ColorConvertFloat4ToU32(new(0f / 255f, 174f / 255f, 255f / 255f, 1f)), "\u2002\u2514");
		ImGui.SameLine();
		ImGui.SetCursorPosY(oldCursor.Y);
	}

	#endregion

	#region Configuration: Notice

	public static void DrawInformationNotice(string message, object id) => DrawNotice(message, FontAwesomeIcon.InfoCircle, Style.Colors.Information, id);
	public static void DrawAlertNotice(string message, object id) => DrawNotice(message, FontAwesomeIcon.ExclamationCircle, Style.Colors.Alert, id);
	public static void DrawErrorNotice(string message, object id) => DrawNotice(message, FontAwesomeIcon.Times, Style.Colors.Error, id);

	public static void DrawNotice(string message, FontAwesomeIcon icon, PluginConfigColor color, object id)
	{
		ImGuiStylePtr style = ImGui.GetStyle();
		Vector2 size = new(ImGui.GetContentRegionAvail().X, 0);
		size.Y += ImGui.CalcTextSize(message, false, size.X - ImGui.GetTextLineHeight() - 2 * style.FramePadding.X - style.ScrollbarSize).Y;
		size.Y += ImGui.GetFrameHeightWithSpacing() - 3f;

		ImGui.PushStyleColor(ImGuiCol.ChildBg, color.Background);
		ImGui.PushStyleColor(ImGuiCol.Border, color.Vector.AddTransparency(0.5f));

		if (ImGui.BeginChild($"##SezzUI_Notice{id}", size, true))
		{
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.AlignTextToFramePadding();
			ImGui.TextColored(color.Vector, icon.ToIconString());
			ImGui.PopFont();
			ImGui.SameLine();
			ImGui.TextWrapped(message);
			ImGui.EndChild();
		}

		ImGui.PopStyleColor(2);
	}

	#endregion
}