using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;

namespace DelvUI.Helpers
{
    public class FontsManager : IDisposable
    {
        #region Singleton
        private FontsManager(string basePath)
        {
            DefaultFontsPath = Path.GetDirectoryName(basePath) + "\\Media\\Fonts\\";
        }

        public static void Initialize(string basePath)
        {
            Instance = new FontsManager(basePath);
        }

        public static FontsManager Instance { get; private set; } = null!;

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

            Instance = null!;
        }
        #endregion

        public readonly string DefaultFontsPath;

        public bool DefaultFontBuilt { get; private set; }
        public ImFontPtr DefaultFont { get; private set; } = null;

        public Dictionary<String, ImFontPtr> Fonts = new Dictionary<string, ImFontPtr>();

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
            if (fontId == null || !Fonts.ContainsKey(fontId))
            {
                return false;
            }

            ImGui.PushFont(Fonts[fontId]);
            return true;
        }

        public unsafe void BuildFonts()
        {
            Fonts.Clear();
            DefaultFontBuilt = false;

            ImGuiIOPtr io = ImGui.GetIO();

            string path = DefaultFontsPath + "MyriadProLightCond.ttf";
            try
            {
                PluginLog.Verbose($"Loading font MyriadProLightCond_24 from path: {path}");
                ImFontPtr fontBig = io.Fonts.AddFontFromFileTTF(path, 24);
                Fonts.Add("MyriadProLightCond_24", fontBig);
                DefaultFont = fontBig;
                DefaultFontBuilt = true;

                PluginLog.Verbose($"Loading font MyriadProLightCond_20 from path: {path}");
                ImFontPtr fontMedim = io.Fonts.AddFontFromFileTTF(path, 20);
                Fonts.Add("MyriadProLightCond_20", fontMedim);

                PluginLog.Verbose($"Loading font MyriadProLightCond_16 from path: {path}");
                ImFontPtr fontSmall = io.Fonts.AddFontFromFileTTF(path, 16);
                Fonts.Add("MyriadProLightCond_16", fontSmall);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Font failed to load: {path}");
            }
        }
    }
}
