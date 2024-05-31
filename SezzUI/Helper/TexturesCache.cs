using System;
using System.Collections.Concurrent;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Ipc;
using Lumina.Excel;
using SezzUI.Logging;
using SezzUI.Modules;

namespace SezzUI.Helper;

public class TexturesCache : IPluginDisposable
{
	private readonly ConcurrentDictionary<uint, IDalamudTextureWrap> _cache = new();
	private readonly ConcurrentDictionary<string, IDalamudTextureWrap> _pathCache = new();
	private readonly ICallGateSubscriber<string, string> _penumbraPathResolver;
	internal PluginLogger Logger;

	public IDalamudTextureWrap? GetTexture<T>(uint rowId, uint stackCount = 0, bool hdIcon = true) where T : ExcelRow
	{
		ExcelSheet<T>? sheet = Services.Data.GetExcelSheet<T>();

		return sheet == null ? null : GetTexture<T>(sheet.GetRow(rowId), stackCount, hdIcon);
	}

	public IDalamudTextureWrap? GetTexture<T>(dynamic? row, uint stackCount = 0, bool hdIcon = true) where T : ExcelRow
	{
		if (row == null)
		{
			return null;
		}

		dynamic? iconId = row.Icon;
		return GetTextureFromIconId(iconId, stackCount, hdIcon);
	}

	public IDalamudTextureWrap? GetTextureFromIconId(uint iconId, uint stackCount = 0, bool hdIcon = true)
	{
		if (_cache.TryGetValue(iconId + stackCount, out IDalamudTextureWrap? texture))
		{
			return texture;
		}

		IDalamudTextureWrap? newTexture = LoadTexture(iconId + stackCount, hdIcon);
		if (newTexture == null)
		{
			return null;
		}

		if (!_cache.TryAdd(iconId + stackCount, newTexture))
		{
			Logger.Debug($"Failed to cache texture #{iconId + stackCount}.");
		}

		return newTexture;
	}

	public IDalamudTextureWrap? GetTextureFromPath(string path)
	{
		if (_pathCache.TryGetValue(path, out IDalamudTextureWrap? texture))
		{
			return texture;
		}

		IDalamudTextureWrap? newTexture = LoadTexture(path);
		if (newTexture == null)
		{
			return null;
		}

		if (!_pathCache.TryAdd(path, newTexture))
		{
			Logger.Debug($"Failed to cache texture path {path}.");
		}

		return newTexture;
	}

	private IDalamudTextureWrap? LoadTexture(uint id, bool hdIcon)
	{
		string hdString = hdIcon ? "_hr1" : "";
		string path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}{hdString}.tex";

		return LoadTexture(path);
	}

	private IDalamudTextureWrap? LoadTexture(string path)
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
		ExcelSheet<T>? sheet = Services.Data.GetExcelSheet<T>();

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
				Logger.Debug($"Failed to remove cached texture #{iconId}.");
			}
		}
	}

	public void RemoveTexture(string path)
	{
		if (_pathCache.ContainsKey(path))
		{
			if (!_pathCache.TryRemove(path, out _))
			{
				Logger.Debug($"Failed to remove cached texture path {path}.");
			}
		}
	}

	public void Clear()
	{
		_cache.Clear();
		_pathCache.Clear();
	}

	public TexturesCache()
	{
		Logger = new(GetType().Name);
		_penumbraPathResolver = Services.PluginInterface.GetIpcSubscriber<string, string>("Penumbra.ResolveDefaultPath");
	}

	bool IPluginDisposable.IsDisposed { get; set; } = false;

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
		if (!disposing || (this as IPluginDisposable).IsDisposed)
		{
			return;
		}

		foreach (uint key in _cache.Keys)
		{
			IDalamudTextureWrap? tex = _cache[key];
			tex?.Dispose();
		}

		_cache.Clear();

		(this as IPluginDisposable).IsDisposed = true;
	}
}