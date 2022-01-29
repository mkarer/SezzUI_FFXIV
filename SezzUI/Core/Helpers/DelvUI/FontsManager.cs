using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using SezzUI;
using SezzUI.Config;
using SezzUI.Interface.GeneralElements;

namespace DelvUI.Helpers
{
	public class FontsManager : IDisposable
	{
		#region Singleton

		private FontsManager(string basePath)
		{
			Logger = new(GetType().Name);
			DefaultFontsPath = Path.GetDirectoryName(basePath) + "\\Media\\Fonts\\";
		}

		public static void Initialize(string basePath)
		{
			Instance = new(basePath);
		}

		public static FontsManager Instance { get; private set; } = null!;
		private FontsConfig? _config;
		internal PluginLogger Logger;

		public void LoadConfig()
		{
			if (_config != null)
			{
				return;
			}

			_config = ConfigurationManager.Instance.GetConfigObject<FontsConfig>();
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
		}

		private void OnConfigReset(ConfigurationManager sender)
		{
			_config = sender.GetConfigObject<FontsConfig>();
		}

		~FontsManager()
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

			ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
			Instance = null!;
		}

		#endregion

		public readonly string DefaultFontsPath;

		public bool DefaultFontBuilt { get; private set; }
		public ImFontPtr DefaultFont { get; private set; } = null;

		private readonly List<ImFontPtr> _fonts = new();
		public IReadOnlyCollection<ImFontPtr> Fonts => _fonts.AsReadOnly();

		public bool PushDefaultFont()
		{
			if (DefaultFontBuilt)
			{
				ImGui.PushFont(DefaultFont);
				return true;
			}

			return false;
		}

		public bool PushFont(string? fontId)
		{
			if (fontId == null || _config == null || !_config.Fonts.ContainsKey(fontId))
			{
				return false;
			}

			int index = _config.Fonts.IndexOfKey(fontId);
			if (index < 0 || index >= _fonts.Count)
			{
				return false;
			}

			ImGui.PushFont(_fonts[index]);
			return true;
		}

		public void BuildFonts()
		{
			_fonts.Clear();
			DefaultFontBuilt = false;

			FontsConfig config = ConfigurationManager.Instance.GetConfigObject<FontsConfig>();
			ImGuiIOPtr io = ImGui.GetIO();
			ImVector? ranges = GetCharacterRanges(config, io);

			foreach (KeyValuePair<string, FontData> fontData in config.Fonts)
			{
				string path = DefaultFontsPath + fontData.Value.Name + ".ttf";
				if (!File.Exists(path))
				{
					path = config.ValidatedFontsPath + fontData.Value.Name + ".ttf";
					if (!File.Exists(path))
					{
						continue;
					}
				}

				try
				{
					ImFontPtr font = ranges == null ? io.Fonts.AddFontFromFileTTF(path, fontData.Value.Size) : io.Fonts.AddFontFromFileTTF(path, fontData.Value.Size, null, ranges.Value.Data);
					_fonts.Add(font);

					if (fontData.Key == FontsConfig.DefaultMediumFontKey)
					{
						DefaultFont = font;
						DefaultFontBuilt = true;
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "BuildFonts", $"Error loading fonts ({path}): {ex}");
				}
			}
		}

		private unsafe ImVector? GetCharacterRanges(FontsConfig config, ImGuiIOPtr io)
		{
			if (!config.SupportChineseCharacters && !config.SupportKoreanCharacters)
			{
				return null;
			}

			ImFontGlyphRangesBuilderPtr builder = new(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());

			if (config.SupportChineseCharacters)
			{
				// GetGlyphRangesChineseFull() includes Default + Hiragana, Katakana, Half-Width, Selection of 1946 Ideographs
				// https://skia.googlesource.com/external/github.com/ocornut/imgui/+/v1.53/extra_fonts/README.txt
				builder.AddRanges(io.Fonts.GetGlyphRangesChineseFull());
			}

			if (config.SupportKoreanCharacters)
			{
				builder.AddRanges(io.Fonts.GetGlyphRangesKorean());
			}

			builder.BuildRanges(out ImVector ranges);

			return ranges;
		}
	}
}