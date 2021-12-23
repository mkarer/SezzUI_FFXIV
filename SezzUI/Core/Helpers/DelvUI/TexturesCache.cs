using Dalamud.Plugin.Ipc;
using ImGuiScene;
using Lumina.Excel;
using System;
using System.Collections.Concurrent;
using Dalamud.Logging;

namespace DelvUI.Helpers
{
    public class TexturesCache : IDisposable
    {
        private ConcurrentDictionary<uint, TextureWrap> _cache = new();
        private ConcurrentDictionary<string, TextureWrap> _pathCache = new();

        public TextureWrap? GetTexture<T>(uint rowId, uint stackCount = 0, bool hdIcon = true) where T : ExcelRow
        {
            var sheet = SezzUI.Plugin.DataManager.GetExcelSheet<T>();

            return sheet == null ? null : GetTexture<T>(sheet.GetRow(rowId), stackCount, hdIcon);
        }

        public TextureWrap? GetTexture<T>(dynamic? row, uint stackCount = 0, bool hdIcon = true) where T : ExcelRow
        {
            if (row == null)
            {
                return null;
            }

            var iconId = row.Icon;
            return GetTextureFromIconId(iconId, stackCount, hdIcon);
        }

        public TextureWrap? GetTextureFromIconId(uint iconId, uint stackCount = 0, bool hdIcon = true)
        {
            if (_cache.TryGetValue(iconId + stackCount, out var texture))
            {
                return texture;
            }

            var newTexture = LoadTexture(iconId + stackCount, hdIcon);
            if (newTexture == null)
            {
                return null;
            }

            if (!_cache.TryAdd(iconId + stackCount, newTexture)) { PluginLog.Debug($"{this.GetType().Name} Failed to cache texture #{iconId + stackCount}."); }

            return newTexture;
        }

        public TextureWrap? GetTextureFromPath(string path)
        {
            if (_pathCache.TryGetValue(path, out var texture))
            {
                return texture;
            }

            var newTexture = LoadTexture(path);
            if (newTexture == null)
            {
                return null;
            }

            if (!_pathCache.TryAdd(path, newTexture)) { PluginLog.Debug($"{this.GetType().Name} Failed to cache texture path {path}."); }

            return newTexture;
        }

        private unsafe TextureWrap? LoadTexture(uint id, bool hdIcon)
        {
            var hdString = hdIcon ? "_hr1" : "";
            var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}{hdString}.tex";

            return LoadTexture(path);
        }

        private unsafe TextureWrap? LoadTexture(string path)
        {
            try
            {
                var resolvedPath = _penumbraPathResolver.InvokeFunc(path);

                if (resolvedPath != null && resolvedPath != path)
                {
                    return TextureLoader.LoadTexture(resolvedPath, true);
                }
            }
            catch { }

            try
            {
                return TextureLoader.LoadTexture(path, false);
            }
            catch { }

            return null;
        }

        private void RemoveTexture<T>(uint rowId) where T : ExcelRow
        {
            var sheet = SezzUI.Plugin.DataManager.GetExcelSheet<T>();

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

            var iconId = row!.Icon;
            RemoveTexture(iconId);
        }

        public void RemoveTexture(uint iconId)
        {
            if (_cache.ContainsKey(iconId))
            {
                if (!_cache.TryRemove(iconId, out _)) { PluginLog.Debug($"{this.GetType().Name} Failed to remove cached texture #{iconId}."); }
            }
        }

        public void RemoveTexture(string path)
        {
            if (_pathCache.ContainsKey(path))
            {
                if (!_pathCache.TryRemove(path, out _)) { PluginLog.Debug($"{this.GetType().Name} Failed to remove cached texture path {path}."); }
            }
        }

        public void Clear()
        {
            _cache.Clear();
            _pathCache.Clear();
        }

        #region Singleton
        private ICallGateSubscriber<string, string> _penumbraPathResolver;

        private TexturesCache()
        {
            _penumbraPathResolver = SezzUI.Plugin.PluginInterface.GetIpcSubscriber<string, string>("Penumbra.ResolveDefaultPath");
        }

        public static void Initialize() { Instance = new TexturesCache(); }

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

            foreach (var key in _cache.Keys)
            {
                var tex = _cache[key];
                tex?.Dispose();
            }

            _cache.Clear();

            Instance = null!;
        }
        #endregion
    }
}
