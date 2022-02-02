using System;
using System.Collections.Concurrent;
using Dalamud.Plugin.Ipc;
using ImGuiScene;
using Lumina.Excel;

namespace SezzUI.Helpers
{
	public class TexturesCache : IDisposable
	{
		private readonly ConcurrentDictionary<uint, TextureWrap> _cache = new();
		private readonly ConcurrentDictionary<string, TextureWrap> _pathCache = new();
		internal PluginLogger Logger;

		public TextureWrap? GetTexture<T>(uint rowId, uint stackCount = 0, bool hdIcon = true) where T : ExcelRow
		{
			ExcelSheet<T>? sheet = Plugin.DataManager.GetExcelSheet<T>();

			return sheet == null ? null : GetTexture<T>(sheet.GetRow(rowId), stackCount, hdIcon);
		}

		public TextureWrap? GetTexture<T>(dynamic? row, uint stackCount = 0, bool hdIcon = true) where T : ExcelRow
		{
			if (row == null)
			{
				return null;
			}

			dynamic? iconId = row.Icon;
			return GetTextureFromIconId(iconId, stackCount, hdIcon);
		}

		public TextureWrap? GetTextureFromIconId(uint iconId, uint stackCount = 0, bool hdIcon = true)
		{
			if (_cache.TryGetValue(iconId + stackCount, out TextureWrap? texture))
			{
				return texture;
			}

			TextureWrap? newTexture = LoadTexture(iconId + stackCount, hdIcon);
			if (newTexture == null)
			{
				return null;
			}

			if (!_cache.TryAdd(iconId + stackCount, newTexture))
			{
				Logger.Debug("GetTextureFromIconId", $"Failed to cache texture #{iconId + stackCount}.");
			}

			return newTexture;
		}

		public TextureWrap? GetTextureFromPath(string path)
		{
			if (_pathCache.TryGetValue(path, out TextureWrap? texture))
			{
				return texture;
			}

			TextureWrap? newTexture = LoadTexture(path);
			if (newTexture == null)
			{
				return null;
			}

			if (!_pathCache.TryAdd(path, newTexture))
			{
				Logger.Debug("GetTextureFromPath", $"Failed to cache texture path {path}.");
			}

			return newTexture;
		}

		private TextureWrap? LoadTexture(uint id, bool hdIcon)
		{
			string hdString = hdIcon ? "_hr1" : "";
			string path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}{hdString}.tex";

			return LoadTexture(path);
		}

		private TextureWrap? LoadTexture(string path)
		{
			try
			{
				string? resolvedPath = _penumbraPathResolver.InvokeFunc(path);

				if (resolvedPath != path)
				{
					return TextureLoader.LoadTexture(resolvedPath, true);
				}
			}
			catch
			{
				//
			}

			try
			{
				return TextureLoader.LoadTexture(path, false);
			}
			catch
			{
				//
			}

			return null;
		}

		private void RemoveTexture<T>(uint rowId) where T : ExcelRow
		{
			ExcelSheet<T>? sheet = Plugin.DataManager.GetExcelSheet<T>();

			if (sheet == null)
			{
				return;
			}

			RemoveTexture<T>(sheet.GetRow(rowId));
		}

		public void RemoveTexture<T>(dynamic? row) where T : ExcelRow
		{
			if (row == null || row?.Icon == null)
			{
				return;
			}

			dynamic? iconId = row!.Icon;
			RemoveTexture(iconId);
		}

		public void RemoveTexture(uint iconId)
		{
			if (_cache.ContainsKey(iconId))
			{
				if (!_cache.TryRemove(iconId, out _))
				{
					Logger.Debug("RemoveTexture", $"Failed to remove cached texture #{iconId}.");
				}
			}
		}

		public void RemoveTexture(string path)
		{
			if (_pathCache.ContainsKey(path))
			{
				if (!_pathCache.TryRemove(path, out _))
				{
					Logger.Debug("RemoveTexture", $"Failed to remove cached texture path {path}.");
				}
			}
		}

		public void Clear()
		{
			_cache.Clear();
			_pathCache.Clear();
		}

		#region Singleton

		private readonly ICallGateSubscriber<string, string> _penumbraPathResolver;

		private TexturesCache()
		{
			Logger = new(GetType().Name);
			_penumbraPathResolver = Plugin.PluginInterface.GetIpcSubscriber<string, string>("Penumbra.ResolveDefaultPath");
		}

		public static void Initialize()
		{
			Instance = new();
		}

		public static TexturesCache Instance { get; private set; } = null!;

		~TexturesCache()
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

			foreach (uint key in _cache.Keys)
			{
				TextureWrap? tex = _cache[key];
				tex?.Dispose();
			}

			_cache.Clear();

			Instance = null!;
		}

		#endregion
	}
}