using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using SezzUI.Logging;
using SezzUI.Modules;

namespace SezzUI.Helper;

public class ImageCache : IPluginDisposable
{
	private readonly ConcurrentDictionary<string, ISharedImmediateTexture> _cache = new();
	internal PluginLogger Logger;

	[Obsolete("Don't use!", true)]
	public IDalamudTextureWrap? GetImage(string? file)
	{
		if (file == null)
		{
			return null;
		}

		if (_cache.TryGetValue(file, out ISharedImmediateTexture? cachedTexture) && cachedTexture != null)
		{
			return cachedTexture.GetWrapOrDefault();
		}

		ISharedImmediateTexture? newTexture = LoadImage(file);
		if (newTexture != null)
		{
			if (!_cache.TryAdd(file, newTexture))
			{
				Logger.Error($"Failed to cache texture: {file}.");
			}
		}
		else
		{
			Logger.Error($"Failed to load texture: {file}");
		}

		return newTexture?.GetWrapOrDefault();
	}

	private ISharedImmediateTexture? LoadImage(string file)
	{
		try
		{
			if (File.Exists(file))
			{
				Logger.Debug($"Loading texture: {file}");
				return Services.TextureProvider.GetFromFile(file);
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
			Logger.Debug($"Removing texture from cache: {file}.");
		}
#endif
		//_cache[file]?.Dispose();
		if (!_cache.TryRemove(file, out _))
		{
			Logger.Debug($"Failed to remove cached texture: {file}.");
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

	public ImageCache()
	{
		Logger = new(GetType().Name);
	}

	bool IPluginDisposable.IsDisposed { get; set; } = false;

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
		if (!disposing || (this as IPluginDisposable).IsDisposed)
		{
			return;
		}

		Remove(_cache.Keys);
		_cache.Clear();

		(this as IPluginDisposable).IsDisposed = true;
	}
}