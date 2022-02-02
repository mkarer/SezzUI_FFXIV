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
		private readonly GeneralConfig _config;
		internal static PluginLogger Logger = null!;

		private readonly string _defaultPath;

		public string BackdropGlowTexture { get; }

		public string[] BorderGlowTexture { get; }

		// TODO: PathChanged Event

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
			if (FileSystemHelper.ValidateFile(_config.Media.Path, fileName, out string validatedCustomFilePath))
			{
				return validatedCustomFilePath;
			}

			if (FileSystemHelper.ValidateFile(_defaultPath, fileName, out string validatedDefaultFilePath))
			{
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

			_config = ConfigurationManager.Instance.GetConfigObject<GeneralConfig>();
			ConfigurationManager.Instance.Reset += OnConfigReset;
			_config.Media.ValueChangeEvent += OnConfigPropertyChanged;

			_defaultPath = defaultPath;
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
			_config.Media.ValueChangeEvent -= OnConfigPropertyChanged;
			Instance = null!;
		}

		#endregion

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			if (sender is not GeneralMediaConfig)
			{
				return;
			}

			switch (args.PropertyName)
			{
				case "Path":
					// TODO: Remove all cached files.
					break;
			}
		}

		private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
		{
			if (config is not GeneralConfig)
			{
			}
		}

		#endregion
	}
}