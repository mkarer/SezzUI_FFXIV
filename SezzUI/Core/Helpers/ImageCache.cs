using ImGuiScene;
using System;
using System.IO;
using System.Collections.Generic;

namespace SezzUI.Helpers
{
    public class ImageCache : IDisposable
    {
        private Dictionary<string, TextureWrap> _pathCache = new();

        public TextureWrap? GetImageFromPath(string path)
        {
            if (_pathCache.ContainsKey(path))
            {
                return _pathCache[path];
            }

            TextureWrap? newTexture = LoadImage(path);
            if (newTexture == null)
            {
                return null;
            }

            _pathCache.Add(path, newTexture);

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
            catch { }

            return null;
        }

        #region Singleton
        private ImageCache()
        {
        }

        public static void Initialize() { Instance = new ImageCache(); }

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

            foreach (var path in _pathCache.Keys)
            {
                var tex = _pathCache[path];
                tex?.Dispose();
            }

            _pathCache.Clear();

            Instance = null!;
        }
        #endregion
    }
}
