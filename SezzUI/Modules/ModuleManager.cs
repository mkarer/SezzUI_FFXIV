using Dalamud.Interface;
using DelvUI.Helpers;
using ImGuiNET;
using System;
using System.Numerics;

namespace SezzUI
{
    internal class ModuleManager : IDisposable
    {
        internal static Modules.GameUI.ElementHider ElementHider { get { return Modules.GameUI.ElementHider.Instance; } }

        public static void Draw()
        {
            if (!FontsManager.Instance.DefaultFontBuilt)
            {
                Plugin.UiBuilder.RebuildFonts();
            }

            TooltipsHelper.Instance.RemoveTooltip(); // remove tooltip from previous frame

            ClipRectsHelper.Instance.Update();

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

            var begin = ImGui.Begin(
                "SezzUI_Modules",
                ImGuiWindowFlags.NoTitleBar
              | ImGuiWindowFlags.NoScrollbar
              | ImGuiWindowFlags.AlwaysAutoResize
              | ImGuiWindowFlags.NoBackground
              | ImGuiWindowFlags.NoInputs
              | ImGuiWindowFlags.NoBringToFrontOnFocus
              | ImGuiWindowFlags.NoSavedSettings
            );

            if (!begin)
            {
                ImGui.End();
                return;
            }

            ElementHider.Draw();
     
            ImGui.End();
        }

        #region Singleton
        public static void Initialize() { Instance = new ModuleManager(); }

        public static ModuleManager Instance { get; private set; } = null!;

        public ModuleManager()
        {
            Modules.GameUI.ElementHider.Initialize();
        }

        ~ModuleManager()
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

            if (ElementHider != null) { ElementHider.Dispose(); }

            Instance = null!;
        }
        #endregion
    }
}
