using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Utility;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Helpers;
using SezzUI.Interface.GeneralElements;

// ReSharper disable InconsistentlySynchronizedField

namespace SezzUI
{
	public class MediaManager : IDisposable
	{
		public GeneralMediaConfig Config { get; }

		internal PluginLogger Logger;

		private readonly string _defaultPath;
		private string _customPath;

		public static readonly Dictionary<string, FontData> ImGuiFontData = new(); // Available Fonts (Plugin)
		public static readonly Dictionary<string, ImFontPtr> ImGuiFonts = new(); // Available Fonts (Plugin)
		public static readonly List<FontFile> FontFiles = new(); // Available Fonts (Filesystem)

		private const string DEFAULT_FONT = "CabinCondensed-SemiBold";

		public static readonly Dictionary<PluginFontSize, string> DefaultFonts = new()
		{
			{PluginFontSize.ExtraExtraSmall, $"{DEFAULT_FONT}_14"},
			{PluginFontSize.ExtraSmall, $"{DEFAULT_FONT}_16"},
			{PluginFontSize.Small, $"{DEFAULT_FONT}_18"},
			{PluginFontSize.Medium, $"{DEFAULT_FONT}_21"},
			{PluginFontSize.Large, $"{DEFAULT_FONT}_23"}
		};

		private static Dictionary<PluginFontSize, string> _userAssignedFonts = new();

		public delegate void PathChangedDelegate(string path);

		public event PathChangedDelegate? PathChanged;

		public delegate void FontAssignmentsChangedDelegate();

		public event FontAssignmentsChangedDelegate? FontAssignmentsChanged;

		public string BackdropGlowTexture { get; }

		public string[] BorderGlowTexture { get; }

		#region Files

		public string? GetOverlayFile(string fileName, bool allowOverride = true)
		{
			if (fileName.IsNullOrWhitespace() || !fileName.ToLower().EndsWith(".png"))
			{
				return null;
			}

			return GetFile(Path.Combine("Images", "Overlays", fileName), allowOverride);
		}

		public string? GetIconFile(string fileName, bool allowOverride = true)
		{
			if (fileName.IsNullOrWhitespace() || !fileName.ToLower().EndsWith(".png"))
			{
				return null;
			}

			return GetFile(Path.Combine("Icons", fileName), allowOverride);
		}

		public string? GetFontFile(string fileName, bool allowOverride = true)
		{
			if (fileName.IsNullOrWhitespace() || !fileName.ToLower().EndsWith(".ttf") && !fileName.ToLower().EndsWith(".otf"))
			{
				return null;
			}

			return GetFile(Path.Combine("Fonts", fileName), allowOverride);
		}

		public string? GetMediaFile(string fileName, bool allowOverride = true) => !fileName.IsNullOrWhitespace() ? GetFile(fileName, allowOverride) : null;

		/// <summary>
		///     Tries to lookup a file in custom media path and default media path.
		/// </summary>
		/// <param name="fileName">Name of the file to lookup, may include relative path.</param>
		/// <param name="allowOverride">Lookup file in custom media path.</param>
		/// <returns>NULL if lookup failed, otherwise the full path.</returns>
		private string? GetFile(string? fileName, bool allowOverride = true)
		{
			if (fileName == null)
			{
				return null;
			}

#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
			{
				Logger.Debug("GetCustomFile", $"File: {fileName} allowOverride: {allowOverride}");
			}
#endif
			if (allowOverride && FileSystemHelper.ValidateFile(Path.Combine(Config.Media.Path, fileName), out string validatedCustomFilePath))
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
				{
					Logger.Debug("GetCustomFile", $"File: {fileName} -> {validatedCustomFilePath}");
				}
#endif
				return validatedCustomFilePath;
			}

			if (FileSystemHelper.ValidateFile(Path.Combine(_defaultPath, fileName), out string validatedDefaultFilePath))
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
				{
					Logger.Debug("GetCustomFile", $"File: {fileName} -> {validatedDefaultFilePath}");
				}
#endif
				return validatedDefaultFilePath;
			}

			return null;
		}

		public static IEnumerable<string> GetFiles(string? path, string searchPattern = "*")
		{
			if (string.IsNullOrEmpty(path))
			{
				return new string[0];
			}

			try
			{
				return Directory.GetFiles(path, searchPattern);
			}
			catch
			{
				return new string[0];
			}
		}

		#endregion

		#region Fonts

		public static FontScope PushFont(PluginFontSize pluginFontSize = PluginFontSize.Medium)
		{
			if (_userAssignedFonts.TryGetValue(pluginFontSize, out string? userAssignFontKey))
			{
				return PushFont(userAssignFontKey);
			}

			return PushFont(DefaultFonts[pluginFontSize]);
		}

		public static FontScope PushFont(string? fontKey)
		{
			if (string.IsNullOrEmpty(fontKey))
			{
				fontKey = DefaultFonts[PluginFontSize.Medium];
			}

			if (!ImGuiFonts.ContainsKey(fontKey))
			{
				return new(false);
			}

			ImGui.PushFont(ImGuiFonts[fontKey]);
			return new(true);
		}

		public void RemoveFont(FontData fontData)
		{
			string fontKey = ImGuiFontData.FirstOrDefault(x => x.Value.Equals(fontData)).Key;
			if (!fontKey.IsNullOrEmpty())
			{
				ImGuiFonts.Remove(fontKey);
				ImGuiFontData.Remove(fontKey);
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
				{
					Logger.Debug("RemoveFont", "Success");
				}
#endif
				Plugin.UiBuilder.RebuildFonts();
			}
			else
			{
				Logger.Error("RemoveFont", "Error: Invalid font data!");
			}
		}

		public static void RemoveCustomFonts()
		{
			if (ImGuiFontData.Any(x => x.Value.Source == MediaSource.Custom))
			{
				foreach ((string fontKey, _) in ImGuiFontData.Where(x => x.Value.Source == MediaSource.Custom))
				{
					ImGuiFontData.Remove(fontKey);
				}

				Plugin.UiBuilder.RebuildFonts();
			}
		}

		public bool AddFont(FontData fontData)
		{
			string fontKey = GetFontKey(fontData);
			if (ImGuiFontData.ContainsKey(fontKey))
			{
				Logger.Error("AddFont", $"Error: Font key already exists: {fontKey}");
				return false;
			}

			fontData.Source = MediaSource.Custom;
			ImGuiFontData.Add(fontKey, fontData);
#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
			{
				Logger.Debug("AddFont", $"Success -> Key: {fontKey} Name: {fontData.Name} Size: {fontData.Size} CN/JP: {fontData.Chinese} KR: {fontData.Korean} Path: {fontData.File.Path}");
			}
#endif
			Plugin.UiBuilder.RebuildFonts();
			return true;
		}

		/// <summary>
		///     Adds font data - only for internal/plugin fonts.
		/// </summary>
		/// <param name="fontKey">One of the font keys listed in defaults.</param>
		/// <param name="source">Dalamud or SezzUI</param>
		private void AddFont(string fontKey, MediaSource source)
		{
			if (ImGuiFontData.ContainsKey(fontKey))
			{
				Logger.Error("AddFont", $"Font key already exists: {fontKey}");
				return;
			}

			if (!DefaultFonts.Values.Contains(fontKey))
			{
				Logger.Error("AddFont", $"Only default fonts are allowed which {fontKey} is not.");
				return;
			}

			string[] splits = fontKey.Split("_", StringSplitOptions.RemoveEmptyEntries);
			if (splits.Length == 2 && int.TryParse(splits[1], out int size))
			{
				FontFile fontFile = FontFiles.Where(file => file.Source == source && file.Name == splits[0]).FirstOrDefault();
				if (!fontFile.Equals(default(FontFile)))
				{
					FontData fontData = new(splits[0], size, false, false, fontFile, MediaSource.SezzUI);
					ImGuiFontData.Add(fontKey, fontData);
#if DEBUG
					if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
					{
						Logger.Debug("AddFont", $"Success -> Key: {fontKey} Name: {fontData.Name} Size: {fontData.Size} CN/JP: {fontData.Chinese} KR: {fontData.Korean} Path: {fontData.File.Path}");
					}
#endif
				}
				else
				{
					Logger.Error("AddFont", $"Failed to add nonexistent font: {fontKey}");
				}
			}
			else
			{
				Logger.Error("AddFont", $"Invalid font key: {fontKey}");
			}
		}

		public FontFile? GetFontFile(string fontKey, MediaSource source)
		{
			if (fontKey.Contains("_"))
			{
				string fontName = fontKey.Substring(fontKey.LastIndexOf("_", StringComparison.Ordinal));
				return FontFiles.Where(file => file.Source == source && file.Name == fontName).FirstOrDefault();
			}

			return null;
		}

		public static string GetFontKey(FontData fontData) => $"{fontData.Name}_{fontData.Size}";

		private void BuildFonts()
		{
			ImGuiFonts.Clear();

			ImGuiIOPtr io = ImGui.GetIO();

			foreach ((string? fontKey, FontData fontData) in ImGuiFontData)
			{
				if (!FileSystemHelper.ValidateFile(fontData.File.Path, out _))
				{
					continue;
				}

				try
				{
					ImVector? ranges = GetCharacterRanges(fontData, io);
					ImFontPtr imFont = !ranges.HasValue ? io.Fonts.AddFontFromFileTTF(fontData.File.Path, fontData.Size) : io.Fonts.AddFontFromFileTTF(fontData.File.Path, fontData.Size, null, ranges.Value.Data);
					ImGuiFonts[fontKey] = imFont;
#if DEBUG
					if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
					{
						Logger.Debug("BuildFonts", $"Success -> Key: {fontKey} Font: {fontData.Name} Size: {fontData.Size} FontSource: {fontData.Source} FileSource: {fontData.File.Source} Path: {fontData.File.Path}");
					}
#endif
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "BuildFonts", $"Error loading font {fontData.File.Path}: {ex}");
				}
			}
		}

		private unsafe ImVector? GetCharacterRanges(FontData fontData, ImGuiIOPtr io)
		{
			if (!fontData.Chinese && !fontData.Korean)
			{
				return null;
			}

			ImFontGlyphRangesBuilderPtr builder = new(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());

			if (fontData.Chinese)
			{
				// GetGlyphRangesChineseFull() includes Default + Hiragana, Katakana, Half-Width, Selection of 1946 Ideographs
				// https://skia.googlesource.com/external/github.com/ocornut/imgui/+/v1.53/extra_fonts/README.txt
				builder.AddRanges(io.Fonts.GetGlyphRangesChineseFull());
			}

			if (fontData.Korean)
			{
				builder.AddRanges(io.Fonts.GetGlyphRangesKorean());
			}

			builder.BuildRanges(out ImVector ranges);

			return ranges;
		}

		private void UpdateDalamudFont()
		{
			lock (FontFiles)
			{
				// Dalamud
				if (!FontFiles.Where(font => font.Source == MediaSource.Dalamud).Any())
				{
					(string? dalamudFontFile, _, _) = DalamudHelper.GetDefaultFont();
					if (!string.IsNullOrEmpty(dalamudFontFile))
					{
						FontFiles.Insert(0, new(MediaSource.Dalamud, Path.GetFileNameWithoutExtension(dalamudFontFile), dalamudFontFile));
#if DEBUG
						if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
						{
							Logger.Debug("UpdateDalamudFont", $"Name: {FontFiles[0].Name} Source: {FontFiles[0].Source} Path: {FontFiles[0].Path}");
						}
#endif
					}
				}
			}
		}

		private void UpdateAvailableFonts()
		{
			lock (FontFiles)
			{
				FontFiles.RemoveAll(font => font.Source == MediaSource.Custom);

				// SezzUI
				if (!FontFiles.Where(font => font.Source == MediaSource.SezzUI).Any())
				{
					foreach (string file in GetFiles(Path.Combine(_defaultPath, "Fonts"), "*.?tf"))
					{
						FontFiles.Add(new(MediaSource.SezzUI, Path.GetFileNameWithoutExtension(file), file));
					}
				}

				// Custom
				if (FileSystemHelper.ValidatePath(Path.Combine(Config.Media.Path, "Fonts"), out string validatedCustomFontsPath))
				{
					foreach (string file in GetFiles(validatedCustomFontsPath, "*.?tf"))
					{
						FontFiles.Add(new(MediaSource.Custom, Path.GetFileNameWithoutExtension(file), file));
					}
				}

#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
				{
					Logger.Debug("UpdateAvailableFonts", "Available Font Files:");
					FontFiles.ForEach(file => { Logger.Debug("UpdateAvailableFonts", $"Name: {file.Name} Source: {file.Source} Path: {file.Path}"); });
				}
#endif
			}
		}

		private void OnDraw()
		{
			UpdateDalamudFont();

			// Check if default fonts are built. It did fail once for me, so it might happen again...
			if (!DefaultFonts.Values.All(fontKey => ImGuiFonts.ContainsKey(fontKey)))
			{
#if DEBUG
				foreach (string fontKey in DefaultFonts.Values)
				{
					Logger.Debug("OnDraw", $"Font {fontKey} built: {ImGuiFonts.ContainsKey(fontKey)}");
				}

				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
				{
					Logger.Debug("OnDraw", "Default font aren't available, retrying...");
				}
#endif
				Plugin.UiBuilder.RebuildFonts();
			}
			else
			{
				Plugin.UiBuilder.Draw -= OnDraw;
			}
		}

		#endregion

		#region Singleton

		public static MediaManager Initialize(string assemblyLocation)
		{
			Instance = new($"{assemblyLocation}Media{Path.DirectorySeparatorChar}");
			return Instance;
		}

		public MediaManager(string defaultPath)
		{
			Logger = new("MediaManager");

			Config = ConfigurationManager.Instance.GetConfigObject<GeneralMediaConfig>();
			ConfigurationManager.Instance.Reset += OnConfigReset;
			Config.Media.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Fonts.ValueChangeEvent += OnConfigPropertyChanged;
			Plugin.UiBuilder.Draw += OnDraw;
			Plugin.UiBuilder.BuildFonts += BuildFonts;

			// Images
			if (Config.Media.Path != "" && !Config.Media.Path.EndsWith(Path.DirectorySeparatorChar))
			{
				Config.Media.Path += Path.DirectorySeparatorChar;
			}

			_defaultPath = defaultPath;
			_customPath = Config.Media.Path;
			_userAssignedFonts = Config.Fonts.CustomFontAssignments;

			BackdropGlowTexture = $"{_defaultPath}Images{Path.DirectorySeparatorChar}GlowTex.png";

			BorderGlowTexture = new string[8];
			for (int i = 0; i < BorderGlowTexture.Length; i++)
			{
				BorderGlowTexture[i] = $"{_defaultPath}Images{Path.DirectorySeparatorChar}Animations{Path.DirectorySeparatorChar}DashedRect38_{i + 1}.png";
			}

			// Fonts
			UpdateAvailableFonts();
			foreach ((_, string fontKey) in DefaultFonts)
			{
				AddFont(fontKey, MediaSource.SezzUI);
			}

			foreach ((string fontKey, FontData fontData) in Config.Fonts.CustomFonts)
			{
				if (!AddFont(fontData))
				{
					Config.Fonts.CustomFonts.Remove(fontKey);
				}
			}

			Config.Fonts.UpdateCustomFonts();
			Plugin.UiBuilder.RebuildFonts();
		}

		public static MediaManager Instance { get; private set; } = null!;

		~MediaManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			Plugin.UiBuilder.Draw -= OnDraw;
			ConfigurationManager.Instance.Reset -= OnConfigReset;
			Config.Media.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Fonts.ValueChangeEvent -= OnConfigPropertyChanged;
			Plugin.UiBuilder.BuildFonts -= BuildFonts;

			ImGuiFontData.Clear();
			ImGuiFonts.Clear();
			FontFiles.Clear();

			Instance = null!;
		}

		#endregion

		#region Configuration Events

		private void HandlePathChange()
		{
			if (Config.Media.Path != "" && !Config.Media.Path.EndsWith(Path.DirectorySeparatorChar))
			{
				Config.Media.Path += Path.DirectorySeparatorChar;
			}

			if (_customPath == Config.Media.Path)
			{
				return;
			}

#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
			{
				Logger.Debug("HandlePathChange", $"Path: {Config.Media.Path}");
			}
#endif
			PathChanged?.Invoke(Config.Media.Path); // Let modules update their images before removing them from cache

			ImageCache.Instance.RemovePath(Path.Combine(_customPath, "Icons"));
			ImageCache.Instance.RemovePath(Path.Combine(_customPath, "Images", "Overlays"));

			_customPath = Config.Media.Path;

			UpdateAvailableFonts();
		}

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			if (sender is CustomMediaPathConfig)
			{
				switch (args.PropertyName)
				{
					case "Path":
						HandlePathChange();
						break;
				}
			}
			else if (sender is CustomFontsConfig)
			{
				switch (args.PropertyName)
				{
					case "FontAssignments":
						FontAssignmentsChanged?.Invoke();
						break;
				}
			}
		}

		private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
		{
			if (config is not GeneralMediaConfig)
			{
			}

			HandlePathChange();
		}

		#endregion
	}

	public struct FontData
	{
		public string Name;
		public int Size;
		public bool Chinese;
		public bool Korean;
		public FontFile File;
		public MediaSource Source;

		public FontData(string name, int size, bool chinese, bool korean, FontFile file, MediaSource source)
		{
			Name = name;
			Size = size;
			Chinese = chinese;
			Korean = korean;
			File = file;
			Source = source;
		}
	}

	public struct FontFile
	{
		public MediaSource Source;
		public string Name;
		public string Path;

		public FontFile(MediaSource source, string name, string path)
		{
			Source = source;
			Name = name;
			Path = path;
		}
	}

	public enum MediaSource
	{
		Dalamud = 0,
		SezzUI = 1,
		Custom = 2
	}

	public enum PluginFontSize
	{
		ExtraExtraSmall,
		ExtraSmall,
		Small,
		Medium,
		Large
	}

	public class FontScope : IDisposable
	{
		private readonly bool _fontPushed;

		public FontScope(bool fontPushed)
		{
			_fontPushed = fontPushed;
		}

		public void Dispose()
		{
			if (_fontPushed)
			{
				ImGui.PopFont();
			}
		}
	}
}