using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ImGuiScene;

namespace SezzUI.Helpers
{
	public class ImageCache : IDisposable
	{
		private readonly ConcurrentDictionary<string, TextureWrap> _cache = new();
		internal PluginLogger Logger;

		public TextureWrap? GetImage(string? file)
		{
			if (file == null)
			{
				return null;
			}

			if (_cache.ContainsKey(file))
			{
				return _cache[file];
			}

			TextureWrap? newTexture = LoadImage(file);
			if (newTexture == null)
			{
				return null;
			}

			if (!_cache.TryAdd(file, newTexture))
			{
				Logger.Error("GetImageFromPath", $"Failed to cache texture: {file}.");
			}

			return newTexture;
		}

		private TextureWrap? LoadImage(string file)
		{
			try
			{
				if (File.Exists(file))
				{
					return Plugin.PluginInterface.UiBuilder.LoadImage(file);
				}
			}
			catch
			{
				//
			}

			return null;
		}

		public bool RemovePath(string path)
		{
			string dirSeparator = Regex.Escape(Path.DirectorySeparatorChar.ToString());
			string filePattern = $"^{Regex.Escape(path.TrimEnd(Path.DirectorySeparatorChar))}(?:{dirSeparator}[^{dirSeparator}]*)$";
			string iconOverridePattern = $"^{Regex.Escape(path.TrimEnd(Path.DirectorySeparatorChar))}(?:{dirSeparator}[0-9]+{dirSeparator}[^{dirSeparator}]*)$";
			return Remove(_cache.Keys.Where(file => Regex.IsMatch(file, filePattern) || Regex.IsMatch(file, iconOverridePattern)));
		}

		public bool Remove(string file)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsImageCache)
			{
				Logger.Debug("Remove", $"Removing texture from cache: {file}.");
			}
#endif
			_cache[file]?.Dispose();
			if (!_cache.TryRemove(file, out _))
			{
				Logger.Debug("Remove", $"Failed to remove cached texture: {file}.");
				return false;
			}

			return true;
		}

		public bool Remove(IEnumerable files)
		{
			bool success = true;

			foreach (string file in files)
			{
				success &= Remove(file);
			}

			return success;
		}

		#region Singleton

		private ImageCache()
		{
			Logger = new(GetType().Name);
		}

		public static void Initialize()
		{
			Instance = new();
		}

		public static ImageCache Instance { get; private set; } = null!;

		~ImageCache()
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

			Remove(_cache.Keys);
			_cache.Clear();

			Instance = null!;
		}

		#endregion
	}
}