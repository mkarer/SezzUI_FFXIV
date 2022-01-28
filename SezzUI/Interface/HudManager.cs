using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using DelvUI.Helpers;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.Interface.GeneralElements;
using SezzUI.Modules;
using SezzUI.Modules.CooldownHud;
using SezzUI.Modules.GameUI;
using SezzUI.Modules.JobHud;
using SezzUI.Modules.PluginMenu;

namespace SezzUI.Interface
{
	public class HudManager : IDisposable
	{
		private GridConfig? _gridConfig;
		private HUDOptionsConfig? _hudOptions;
		private DraggableHudElement? _selectedElement;

		private List<PluginModule> _modules = null!;
		private List<DraggableHudElement> _draggableElements = null!;
		
		private List<IHudElementWithActor> _hudElementsUsingPlayer = null!;
		private List<IHudElementWithActor> _hudElementsUsingTarget = null!;
		private List<IHudElementWithActor> _hudElementsUsingTargetOfTarget = null!;
		private List<IHudElementWithActor> _hudElementsUsingFocusTarget = null!;
		private List<IHudElementWithPreview> _hudElementsWithPreview = null!;

		private static JobHud? _jobHud;
		private static CooldownHud? _cooldownHud;
		private static ElementHider? _elementHider;
		private static ActionBar? _actionBar;
		private static PluginMenu? _pluginMenu;

		private readonly HudHelper _hudHelper = new();

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

			_hudHelper.Dispose();

			_jobHud?.Dispose();

			_modules.ForEach(module => module.Dispose());
			_modules.Clear();

			_draggableElements.Clear();
			_hudElementsUsingPlayer.Clear();
			_hudElementsUsingTarget.Clear();
			_hudElementsUsingTargetOfTarget.Clear();
			_hudElementsUsingFocusTarget.Clear();

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

			foreach (DraggableHudElement element in _draggableElements)
			{
				element.DraggingEnabled = draggingEnabled;
				element.Selected = false;
			}

			_selectedElement = null;
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
			foreach (DraggableHudElement element in _draggableElements)
			{
				element.Selected = element == sender;
			}

			_selectedElement = sender;
		}

		private void CreateHudElements()
		{
			_gridConfig = ConfigurationManager.Instance.GetConfigObject<GridConfig>();
			_hudOptions = ConfigurationManager.Instance.GetConfigObject<HUDOptionsConfig>();

			_modules = new();
			_hudElementsUsingPlayer = new();
			_hudElementsUsingTarget = new();
			_hudElementsUsingTargetOfTarget = new();
			_hudElementsUsingFocusTarget = new();
			_hudElementsWithPreview = new();

			CreateModules();
			CreateMiscElements();

			UpdateDraggableElements();
		}

		public void UpdateDraggableElements()
		{
			_draggableElements = new();

			if (_jobHud != null) // TODO
			{
				_draggableElements.Add(_jobHud);
			}

			_modules.ForEach(module => { _draggableElements.AddRange(module.DraggableElements); });

			foreach (DraggableHudElement element in _draggableElements)
			{
				element.SelectEvent += OnDraggableElementSelected;
			}
		}

		private void CreateMiscElements()
		{
			// Job HUD
			if (_jobHud == null)
			{
				_jobHud = new(ConfigurationManager.Instance.GetConfigObject<JobHudConfig>(), "Job HUD");
			}

			_hudElementsUsingPlayer.Add(_jobHud);
		}

		private void CreateModules()
		{
			// Cooldown HUD
			if (_cooldownHud == null)
			{
				_cooldownHud = CooldownHud.Initialize();
			}

			_modules.Add(_cooldownHud);

			// Game UI Tweaks
			if (_actionBar == null)
			{
				// Load this module before ActionBars are getting hidden.
				ActionBar.Initialize();
				_actionBar = ActionBar.Instance;
			}

			_modules.Add(_actionBar);

			if (_elementHider == null)
			{
				ElementHider.Initialize();
				_elementHider = ElementHider.Instance;
			}

			_modules.Add(_elementHider);

			// Plugin Menu
			if (_pluginMenu == null)
			{
				PluginMenu.Initialize();
				_pluginMenu = PluginMenu.Instance;
			}

			_modules.Add(_pluginMenu);
		}

		public void Draw(DrawState drawState)
		{
			if (!FontsManager.Instance.DefaultFontBuilt)
			{
				Plugin.UiBuilder.RebuildFonts();
			}

			TooltipsHelper.Instance.RemoveTooltip(); // remove tooltip from previous frame

			// don't draw hud when it's not supposed to be visible
			if (drawState == DrawState.HiddenNotInGame || drawState == DrawState.HiddenDisabled)
			{
				_modules.ForEach(module => module.Draw(drawState, null));
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

			_hudHelper.Update();

			AssignActors();

			Vector2 origin = ImGui.GetMainViewport().Size / 2f;
			if (_hudOptions is {UseGlobalHudShift: true})
			{
				origin += _hudOptions.HudOffset;
			}

			// don't draw grid during cutscenes or quest events
			if (drawState == DrawState.HiddenCutscene || drawState == DrawState.Partially)
			{
				_modules.ForEach(module => module.Draw(drawState, origin));
				ImGui.End();
				return;
			}

			// draw grid
			if (_gridConfig is not null && _gridConfig.Enabled)
			{
				DraggablesHelper.DrawGrid(_gridConfig, _hudOptions, _selectedElement);
			}

			// draw modules
			_modules.ForEach(module => module.Draw(drawState, origin));

			// draw draggable elements
			lock (_draggableElements)
			{
				DraggablesHelper.DrawElements(origin, _hudHelper, _draggableElements, _selectedElement);
			}

			// draw tooltip
			TooltipsHelper.Instance.Draw();

			ImGui.End();
		}

		private void AssignActors()
		{
			// player
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
			foreach (IHudElementWithActor element in _hudElementsUsingPlayer)
			{
				element.Actor = player;
			}

			// target
			GameObject? target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
			foreach (IHudElementWithActor element in _hudElementsUsingTarget)
			{
				element.Actor = target;
			}

			// target of target
			GameObject? targetOfTarget = Utils.FindTargetOfTarget(target, player, Plugin.ObjectTable);
			foreach (IHudElementWithActor element in _hudElementsUsingTargetOfTarget)
			{
				element.Actor = targetOfTarget;
			}

			// focus
			GameObject? focusTarget = Plugin.TargetManager.FocusTarget;
			foreach (IHudElementWithActor element in _hudElementsUsingFocusTarget)
			{
				element.Actor = focusTarget;
			}
		}
	}
}
