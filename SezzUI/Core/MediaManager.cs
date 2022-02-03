using System;
using System.IO;
using Dalamud.Utility;
using SezzUI.Config;
using SezzUI.Helpers;
using SezzUI.Interface.GeneralElements;

namespace SezzUI
{
	public class MediaManager : IDisposable
	{
		public GeneralMediaConfig Config { get; }

		internal static PluginLogger Logger = null!;

		private readonly string _defaultPath;
		private string _customPath;

		public delegate void PathChangedDelegate(string path);

		public event PathChangedDelegate? PathChanged;

		public string BackdropGlowTexture { get; }

		public string[] BorderGlowTexture { get; }

		public string? GetOverlayFile(string fileName)
		{
			if (fileName.IsNullOrWhitespace() || !fileName.ToLower().EndsWith(".png"))
			{
				return null;
			}

			return GetCustomFile(GetRelativePath(fileName, "Images\\Overlays"));
		}

		public string? GetIconFile(string fileName)
		{
			if (fileName.IsNullOrWhitespace() || !fileName.ToLower().EndsWith(".png"))
			{
				return null;
			}

			return GetCustomFile(GetRelativePath(fileName, "Icons"));
		}

		public string? GetFontFile(string fileName)
		{
			if (fileName.IsNullOrWhitespace() || !fileName.ToLower().EndsWith(".ttf"))
			{
				return null;
			}

			return GetCustomFile(GetRelativePath(fileName, "Fonts"));
		}

		public string? GetMediaFile(string fileName) => !fileName.IsNullOrWhitespace() ? GetCustomFile(fileName.Trim(Path.DirectorySeparatorChar)) : null;

		private string GetRelativePath(string fileName, string path) => $"{path.Trim(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}{fileName.Trim(Path.DirectorySeparatorChar)}";

		/// <summary>
		///     Tries to lookup a file in custom media path and default media path.
		/// </summary>
		/// <param name="fileName">Name of the file to lookup, may include relative path.</param>
		/// <returns>NULL if lookup failed, otherwise full path of the found file.</returns>
		private string? GetCustomFile(string? fileName)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
			{
				Logger.Debug("GetCustomFile", $"File: {fileName}");
			}
#endif
			if (FileSystemHelper.ValidateFile(Config.Media.Path, fileName, out string validatedCustomFilePath))
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsMediaManager)
				{
					Logger.Debug("GetCustomFile", $"File: {fileName} -> {validatedCustomFilePath}");
				}
#endif
				return validatedCustomFilePath;
			}

			if (FileSystemHelper.ValidateFile(_defaultPath, fileName, out string validatedDefaultFilePath))
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

			_defaultPath = defaultPath;
			_customPath = Config.Media.Path;
			BackdropGlowTexture = $"{_defaultPath}Images{Path.DirectorySeparatorChar}GlowTex.png";

			BorderGlowTexture = new string[8];
			for (int i = 0; i < BorderGlowTexture.Length; i++)
			{
				BorderGlowTexture[i] = $"{_defaultPath}Images{Path.DirectorySeparatorChar}Animations{Path.DirectorySeparatorChar}DashedRect38_{i + 1}.png";
			}
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

			ConfigurationManager.Instance.Reset -= OnConfigReset;
			Config.Media.ValueChangeEvent -= OnConfigPropertyChanged;
			Instance = null!;
		}

		#endregion

		#region Configuration Events

		private void HandlePathChange()
		{
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

			ImageCache.Instance.RemovePath(GetRelativePath("Icons", _customPath));
			ImageCache.Instance.RemovePath(GetRelativePath($"Images{Path.DirectorySeparatorChar}Overlays", _customPath));

			_customPath = Config.Media.Path;
		}

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			if (sender is not CustomMediaPathConfig)
			{
				return;
			}

			switch (args.PropertyName)
			{
				case "Path":
					HandlePathChange();
					break;
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
}