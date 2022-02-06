using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.Helpers;
using SezzUI.Interface.GeneralElements;
using SezzUI.Modules;
using SezzUI.Modules.CooldownHud;
using SezzUI.Modules.GameUI;
using SezzUI.Modules.JobHud;
using SezzUI.Modules.PluginMenu;
using SezzUI.Modules.ServerInfoBar;

namespace SezzUI.Interface
{
	public class HudManager : IDisposable
	{
		private GridConfig? _gridConfig;
		private HUDOptionsConfig? _hudOptions;

		private List<BaseModule> _modules = null!;
		private List<DraggableHudElement> _draggableHudElements = null!;
		private DraggableHudElement? _selectedHudElement;

		private List<IHudElementWithPreview> _hudElementsWithPreview = null!;

		private static JobHud? _jobHud;
		private static CooldownHud? _cooldownHud;
		private static ElementHider? _elementHider;
		private static ActionBar? _actionBar;
		private static PluginMenu? _pluginMenu;
		private static ServerInfoBar? _serverInfoBar;

		#region Singleton

		public static void Initialize()
		{
			Instance = new();
		}

		public static HudManager Instance { get; private set; } = null!;

		public HudManager()
		{
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
			ConfigurationManager.Instance.LockEvent += OnHUDLockChanged;
			ConfigurationManager.Instance.ConfigClosedEvent += OnConfigWindowClosed;

			CreateHudElements();
		}

		~HudManager()
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

			_modules.ForEach(module => module.Dispose());
			_modules.Clear();
			_draggableHudElements.Clear();
			_hudElementsWithPreview.Clear();

			ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
			ConfigurationManager.Instance.LockEvent -= OnHUDLockChanged;

			Instance = null!;
		}

		#endregion

		private void OnConfigReset(ConfigurationManager sender)
		{
			CreateHudElements();
		}

		private void OnHUDLockChanged(ConfigurationManager sender)
		{
			bool draggingEnabled = !sender.LockHUD;

			_draggableHudElements.ForEach(element =>
			{
				element.DraggingEnabled = draggingEnabled;
				element.Selected = false;
			});

			_selectedHudElement = null;
		}

		private void OnConfigWindowClosed(ConfigurationManager sender)
		{
			if (_hudOptions == null || !_hudOptions.AutomaticPreviewDisabling)
			{
				return;
			}

			foreach (IHudElementWithPreview element in _hudElementsWithPreview)
			{
				element.StopPreview();
			}
		}

		private void OnDraggableElementSelected(DraggableHudElement sender)
		{
			_draggableHudElements.ForEach(element => element.Selected = element == sender);
			_selectedHudElement = sender;
		}

		private void CreateHudElements()
		{
			_gridConfig = ConfigurationManager.Instance.GetConfigObject<GridConfig>();
			_hudOptions = ConfigurationManager.Instance.GetConfigObject<HUDOptionsConfig>();

			_modules = new();
			_hudElementsWithPreview = new();

			CreateModules();
			UpdateDraggableElements();
		}

		public void UpdateDraggableElements()
		{
			_draggableHudElements = new();

			foreach (BaseModule module in _modules.Where(module => module is PluginModule))
			{
				_draggableHudElements.AddRange(((PluginModule) module).DraggableElements);
			}

			_draggableHudElements.ForEach(element => element.ElementSelected += OnDraggableElementSelected);
		}

		private void CreateModules()
		{
			// Job HUD
			_jobHud ??= JobHud.Initialize();
			_modules.Add(_jobHud);

			// Cooldown HUD
			_cooldownHud ??= CooldownHud.Initialize();
			_modules.Add(_cooldownHud);

			// Game UI Tweaks
			_actionBar ??= ActionBar.Initialize(); // We probably should load this module before ActionBars are getting hidden.
			_modules.Add(_actionBar);

			_elementHider ??= ElementHider.Initialize();
			_modules.Add(_elementHider);

			// Plugin Menu
			_pluginMenu ??= PluginMenu.Initialize();
			_modules.Add(_pluginMenu);

			// Server Info Bar
			_serverInfoBar ??= ServerInfoBar.Initialize();
			_modules.Add(_serverInfoBar);
		}

		public void Draw(DrawState drawState)
		{
			TooltipsHelper.Instance.RemoveTooltip(); // remove tooltip from previous frame

			// don't draw hud when it's not supposed to be visible
			if (drawState == DrawState.HiddenNotInGame || drawState == DrawState.HiddenDisabled)
			{
				_modules.ForEach(module => module.Draw(drawState));
				return;
			}

			ClipRectsHelper.Instance.Update();

			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGui.SetNextWindowPos(Vector2.Zero);
			ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

			bool begin = ImGui.Begin("SezzUI_HUD", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings);

			if (!begin)
			{
				ImGui.End();
				return;
			}

			// don't draw grid during cutscenes or quest events
			if (drawState == DrawState.HiddenCutscene || drawState == DrawState.Partially)
			{
				_modules.ForEach(module => module.Draw(drawState));
				ImGui.End();
				return;
			}

			// draw grid
			if (_gridConfig is not null && _gridConfig.Enabled)
			{
				DraggablesHelper.DrawGrid(_gridConfig, _hudOptions, _selectedHudElement);
			}

			// draw modules
			_modules.ForEach(module => module.Draw(drawState));

			// draw draggable elements
			if (!ConfigurationManager.Instance.LockHUD)
			{
				lock (_draggableHudElements)
				{
					DraggablesHelper.DrawDraggableElements(_draggableHudElements, _selectedHudElement);
				}
			}

			// draw tooltip
			TooltipsHelper.Instance.Draw();

			ImGui.End();
		}
	}
}