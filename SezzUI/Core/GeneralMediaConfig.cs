using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using ImGuiNET;
using Newtonsoft.Json;
using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Helpers;

namespace SezzUI.Interface.GeneralElements
{
	[Disableable(false)]
	[Exportable(false)]
	[Section("General")]
	[SubSection("Media", 0)]
	public class GeneralMediaConfig : PluginConfigObject
	{
		[NestedConfig("Custom Media Path", 20, spacing = false)]
		public CustomMediaPathConfig Media = new();

		[NestedConfig("Fonts", 30, spacing = false, separator = true)]
		public CustomFontsConfig Fonts = new();

		public void Reset()
		{
			Media.Reset();
			Fonts.Reset();
		}

		public GeneralMediaConfig()
		{
			Reset();
		}

		public new static GeneralMediaConfig DefaultConfig() => new();
	}

	[Disableable(false)]
	public class CustomMediaPathConfig : PluginConfigObject
	{
		[SelectFolder("Path", isMonitored = true)]
		[Order(10)]
		public string Path = "";

		[Text("PathHelp", "Subfolder Structure:\nFonts\\*.?tf    >    Fonts\nIcons\\IconPath    >    Status/Action Icon Override\n     Example: IconPath \"/i/013000/013403.png\" would get looked up in MediaPath\\Icons\\013000\\013403.png\n     Additional note: Don't add _hr1 to your icons!\nIcons\\*.png    >    PluginMenu Icons\nImages\\Overlays\\*.png    >    Aura Alert Overlays")]
		[Order(15)]
		[JsonIgnore]
		public string PathHelp = null!;

		public void Reset()
		{
			Path = "";
		}

		public new static CustomMediaPathConfig DefaultConfig() => new();
	}

	[Disableable(false)]
	public class CustomFontsConfig : PluginConfigObject
	{
		public Dictionary<string, FontData> CustomFonts;
		public Dictionary<PluginFontSize, string> CustomFontAssignments;

		[JsonIgnore]
		private readonly string[] _fontSizes = Enumerable.Range(1, 60).Select(i => i.ToString()).ToArray();

		[JsonIgnore]
		private string[] _fontKeys = new string[0];

		[JsonIgnore]
		private string[] _fontOptions = new string[0];

		[JsonIgnore]
		private readonly List<FontFile> _availableFonts = MediaManager.FontFiles;

		[JsonIgnore]
		private int _selectedFontFile;

		[JsonIgnore]
		private int _selectedFontSize = 17;

		[JsonIgnore]
		private bool _supportChinese;

		[JsonIgnore]
		private bool _supportKorean;

		[ManualDraw]
		public bool Draw(ref bool changed)
		{
			if (_selectedFontFile > _availableFonts.Count - 1)
			{
				_selectedFontFile = 0;
			}

			// Add Font
			ImGuiHelper.DrawNestIndicator(1);
			ImGui.PushItemWidth(420);
			ImGui.Combo("##SezzUICustomFontsConfig_FontFile", ref _selectedFontFile, _availableFonts.Select(x => $"{x.Source}: {x.Name}").ToArray(), _availableFonts.Count);
			ImGui.PopItemWidth();
			ImGui.SameLine();
			ImGui.PushItemWidth(60);
			ImGui.Combo("##SezzUICustomFontsConfig_FontSize", ref _selectedFontSize, _fontSizes, _fontSizes.Length);
			ImGui.PopItemWidth();
			ImGui.SameLine();
			if (ImGuiHelper.Button("", FontAwesomeIcon.Plus, "Add Font"))
			{
				// Add Font + Rebuild
				FontFile fontFile = _availableFonts[_selectedFontFile];
				FontData fontData = new(fontFile.Name, int.Parse(_fontSizes[_selectedFontSize]), _supportChinese, _supportKorean, fontFile, MediaSource.Custom);
				MediaManager.Instance.AddFont(fontData);
				UpdateCustomFonts();
				changed = true;
			}

			// Character Support
			ImGuiHelper.DrawNestIndicator(1);
			ImGui.Checkbox("Support Chinese/Japanese", ref _supportChinese);
			ImGui.SameLine();
			ImGui.Checkbox("Support Korean", ref _supportKorean);

			// Font List
			ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInner | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings;

			ImGuiHelper.DrawNestIndicator(1);
			if (ImGui.BeginTable("##SezzUICustomFontsConfig_FontTable", 6, tableFlags, new(519, 260)))
			{
				ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 16, 0);
				ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0, 1);
				ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 40, 2);
				ImGui.TableSetupColumn("CN/JP", ImGuiTableColumnFlags.WidthFixed, 40, 3);
				ImGui.TableSetupColumn("KR", ImGuiTableColumnFlags.WidthFixed, 40, 4);
				ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 23, 5);

				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableHeadersRow();

				for (int i = 0; i < MediaManager.ImGuiFontData.Keys.Count; i++)
				{
					ImGui.PushID(i.ToString());
					ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

					string fontKey = MediaManager.ImGuiFontData.OrderBy(x => x.Value.Name).ThenBy(x => x.Value.Size).Select(x => x.Key).ElementAt(i);
					FontData fontData = MediaManager.ImGuiFontData[fontKey];

					bool fontLoaded = MediaManager.ImGuiFonts.ContainsKey(fontKey);
					if (!fontLoaded)
					{
						ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, 0x4000007f);

						if (ImGui.TableSetColumnIndex(0))
						{
							ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4f);
							ImGui.PushFont(UiBuilder.IconFont);
							ImGui.TextColored(new(1, 216f / 255f, 0, 1), FontAwesomeIcon.ExclamationCircle.ToIconString());
							ImGui.PopFont();
							if (ImGui.IsItemHovered())
							{
								ImGui.SetTooltip("Failed to load font.");
							}
						}
					}

					if (ImGui.TableSetColumnIndex(1))
					{
						ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
						ImGui.Text(fontData.Name);

						if (ImGui.IsItemHovered())
						{
							ImGui.SetTooltip($"Path: {fontData.File.Path}");
						}
					}

					if (ImGui.TableSetColumnIndex(2))
					{
						ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
						ImGui.Text(fontData.Size.ToString());
					}

					if (ImGui.TableSetColumnIndex(3))
					{
						ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
						ImGui.Text(fontData.Chinese ? "Yes" : "No");
					}

					if (ImGui.TableSetColumnIndex(4))
					{
						ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
						ImGui.Text(fontData.Korean ? "Yes" : "No");
					}

					if (ImGui.TableSetColumnIndex(5))
					{
						if (fontData.Source == MediaSource.Custom)
						{
							ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
							if (ImGuiHelper.Button("", FontAwesomeIcon.TrashAlt, "Remove Font"))
							{
								// Remove Font + Rebuild
								MediaManager.Instance.RemoveFont(fontData);
								UpdateCustomFonts();
								changed = true;
							}
						}
					}
				}

				ImGui.EndTable();
			}

			ImGuiHelper.DrawSpacing(1);
			ImGui.Text("Font Assignments");

			ImGuiHelper.DrawNestIndicator(1);
			changed |= DrawFontAssignmentCombo(PluginFontSize.ExtraExtraSmall);

			ImGuiHelper.DrawNestIndicator(1);
			changed |= DrawFontAssignmentCombo(PluginFontSize.ExtraSmall);

			ImGuiHelper.DrawNestIndicator(1);
			changed |= DrawFontAssignmentCombo(PluginFontSize.Small);

			ImGuiHelper.DrawNestIndicator(1);
			changed |= DrawFontAssignmentCombo(PluginFontSize.Medium);

			ImGuiHelper.DrawNestIndicator(1);
			changed |= DrawFontAssignmentCombo(PluginFontSize.Large);

			return false;
		}

		private bool DrawFontAssignmentCombo(PluginFontSize pluginFontSize)
		{
			bool changed = false;

			int selectedIndex = !CustomFontAssignments.ContainsKey(pluginFontSize) || !MediaManager.ImGuiFontData.ContainsKey(CustomFontAssignments[pluginFontSize]) ? -1 : Array.IndexOf(_fontKeys, CustomFontAssignments[pluginFontSize]);
			if (selectedIndex == -1)
			{
				selectedIndex = Array.IndexOf(_fontKeys, MediaManager.DefaultFonts[pluginFontSize]);
				changed = true;
			}

			changed |= ImGui.Combo($"{pluginFontSize}##SezzUICustomFontsConfig_FontAssignment{pluginFontSize}", ref selectedIndex, _fontOptions, _fontOptions.Length);

			if (changed)
			{
				CustomFontAssignments[pluginFontSize] = _fontKeys[selectedIndex];
				OnValueChanged(new OnChangeEventArgs<PluginFontSize>("FontAssignments", pluginFontSize));
			}

			return changed;
		}

		public void UpdateCustomFonts()
		{
			CustomFonts.Clear();
			foreach ((string fontKey, FontData fontData) in MediaManager.ImGuiFontData.Where(x => x.Value.Source == MediaSource.Custom))
			{
				CustomFonts[fontKey] = fontData;
			}

			List<string> fontKeys = new();
			fontKeys.AddRange(MediaManager.ImGuiFontData.OrderBy(x => x.Value.Name).ThenBy(x => x.Value.Size).Select(x => x.Key));
			_fontKeys = fontKeys.ToArray();

			List<string> fontOptions = new();
			fontOptions.AddRange(MediaManager.ImGuiFontData.OrderBy(x => x.Value.Name).ThenBy(x => x.Value.Size).Select(x => $"{x.Value.Name}\u2002\u2002{x.Value.Size}"));
			_fontOptions = fontOptions.ToArray();
		}

		public void Reset()
		{
			CustomFonts.Clear();
			CustomFontAssignments.Clear();
			MediaManager.RemoveCustomFonts();
		}

		public CustomFontsConfig()
		{
			CustomFonts = new();
			CustomFontAssignments = new();
		}

		public new static CustomFontsConfig DefaultConfig() => new();
	}
}