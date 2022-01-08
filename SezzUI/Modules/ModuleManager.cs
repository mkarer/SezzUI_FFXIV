using Dalamud.Interface;
using DelvUI.Helpers;
using ImGuiNET;
using SezzUI.Enums;
using System;
using System.Numerics;

namespace SezzUI
{
    internal class ModuleManager : IDisposable
    {
        internal static Modules.GameUI.ElementHider ElementHider { get { return Modules.GameUI.ElementHider.Instance; } }
        internal static Modules.GameUI.ActionBar ActionBar { get { return Modules.GameUI.ActionBar.Instance; } }

        public static void Draw(DrawState drawState)
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

            ElementHider.Draw(drawState, Vector2.Zero);
            ActionBar.Draw(drawState, Vector2.Zero);

            ImGui.End();
        }

        #region Singleton
        public static void Initialize() { Instance = new ModuleManager(); }

        public static ModuleManager Instance { get; private set; } = null!;

        public ModuleManager()
        {
            Modules.GameUI.ActionBar.Initialize(); // Load this module before ActionBars are getting hidden.
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

        protected static void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            ElementHider?.Dispose();
            ActionBar?.Dispose();

            Instance = null!;
        }
        #endregion
    }
}
