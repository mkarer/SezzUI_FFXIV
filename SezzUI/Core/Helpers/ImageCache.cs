using System;
using System.Collections.Concurrent;
using System.IO;
using ImGuiScene;

namespace SezzUI.Helpers
{
	public class ImageCache : IDisposable
	{
		private readonly ConcurrentDictionary<string, TextureWrap> _pathCache = new();
		internal PluginLogger Logger;

		public TextureWrap? GetImageFromPath(string? path)
		{
			if (path == null)
			{
				return null;
			}

			if (_pathCache.ContainsKey(path))
			{
				return _pathCache[path];
			}

			TextureWrap? newTexture = LoadImage(path);
			if (newTexture == null)
			{
				return null;
			}

			if (!_pathCache.TryAdd(path, newTexture))
			{
				Logger.Error("GetImageFromPath", $"Failed to cache texture path {path}.");
			}

			return newTexture;
		}

		private TextureWrap? LoadImage(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					return Plugin.PluginInterface.UiBuilder.LoadImage(path);
				}
			}
			catch
			{
				//
			}

			return null;
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

			foreach (string path in _pathCache.Keys)
			{
				TextureWrap? tex = _pathCache[path];
				tex?.Dispose();
			}

			_pathCache.Clear();

			Instance = null!;
		}

		#endregion
	}
}