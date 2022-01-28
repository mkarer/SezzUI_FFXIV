using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using SezzUI.Enums;
using SezzUI.Config;
using DelvUI.Helpers;
using SezzUI.Interface.GeneralElements;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SezzUI.Interface
{
	public class HudManager : IDisposable
	{
		private GridConfig? _gridConfig;
		private HUDOptionsConfig? _hudOptions;
		private DraggableHudElement? _selectedElement = null;

		private List<PluginModule> _hudModules = null!;
		private List<DraggableHudElement> _hudElements = null!;
		private List<IHudElementWithActor> _hudElementsUsingPlayer = null!;
		private List<IHudElementWithActor> _hudElementsUsingTarget = null!;
		private List<IHudElementWithActor> _hudElementsUsingTargetOfTarget = null!;
		private List<IHudElementWithActor> _hudElementsUsingFocusTarget = null!;
		private List<IHudElementWithPreview> _hudElementsWithPreview = null!;

		private static Modules.JobHud.JobHud? _jobHud;
		private static Modules.CooldownHud.CooldownHud? _cooldownHud;
		private static Modules.GameUI.ElementHider? _elementHider;
		private static Modules.GameUI.ActionBar? _actionBar;
		private static Modules.PluginMenu.PluginMenu? _pluginMenu;

		private readonly HudHelper _hudHelper = new HudHelper();

		#region Singleton

		public static void Initialize()
		{
			Instance = new HudManager();
		}

		public static HudManager Instance { get; private set; } = null!;

		public HudManager()
		{
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
			ConfigurationManager.Instance.LockEvent += OnHUDLockChanged;
			ConfigurationManager.Instance.ConfigClosedEvent += OnConfingWindowClosed;

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

			_hudModules.ForEach(module => module.Dispose());
			_hudModules.Clear();

			_hudElements.Clear();
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
			var draggingEnabled = !sender.LockHUD;

			foreach (var element in _hudElements)
			{
				element.DraggingEnabled = draggingEnabled;
				element.Selected = false;
			}

			_selectedElement = null;
		}

		private void OnConfingWindowClosed(ConfigurationManager sender)
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
			foreach (var element in _hudElements)
			{
				element.Selected = element == sender;
			}

			_selectedElement = sender;
		}

		private void CreateHudElements()
		{
			_gridConfig = ConfigurationManager.Instance.GetConfigObject<GridConfig>();
			_hudOptions = ConfigurationManager.Instance.GetConfigObject<HUDOptionsConfig>();

			_hudModules = new();
			_hudElements = new List<DraggableHudElement>();
			_hudElementsUsingPlayer = new List<IHudElementWithActor>();
			_hudElementsUsingTarget = new List<IHudElementWithActor>();
			_hudElementsUsingTargetOfTarget = new List<IHudElementWithActor>();
			_hudElementsUsingFocusTarget = new List<IHudElementWithActor>();
			_hudElementsWithPreview = new List<IHudElementWithPreview>();

			CreateModules();
			CreateMiscElements();

			foreach (var element in _hudElements)
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

			_hudElements.Add(_jobHud);
			_hudElementsUsingPlayer.Add(_jobHud);
		}

		private void CreateModules()
		{
			// Cooldown HUD
			if (_cooldownHud == null)
			{
				_cooldownHud = Modules.CooldownHud.CooldownHud.Initialize();
			}

			_hudModules.Add(_cooldownHud);

			// Game UI Tweaks
			if (_actionBar == null)
			{
				// Load this module before ActionBars are getting hidden.
				Modules.GameUI.ActionBar.Initialize();
				_actionBar = Modules.GameUI.ActionBar.Instance;
			}

			_hudModules.Add(_actionBar);

			if (_elementHider == null)
			{
				Modules.GameUI.ElementHider.Initialize();
				_elementHider = Modules.GameUI.ElementHider.Instance;
			}

			_hudModules.Add(_elementHider);

			// Plugin Menu
			if (_pluginMenu == null)
			{
				Modules.PluginMenu.PluginMenu.Initialize();
				_pluginMenu = Modules.PluginMenu.PluginMenu.Instance;
			}

			_hudModules.Add(_pluginMenu);
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
				_hudModules.ForEach(module => module.Draw(drawState, null));
				return;
			}

			ClipRectsHelper.Instance.Update();

			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGui.SetNextWindowPos(Vector2.Zero);
			ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

			var begin = ImGui.Begin("SezzUI_HUD", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings);

			if (!begin)
			{
				ImGui.End();
				return;
			}

			_hudHelper.Update();

			AssignActors();

			var origin = ImGui.GetMainViewport().Size / 2f;
			if (_hudOptions is {UseGlobalHudShift: true})
			{
				origin += _hudOptions.HudOffset;
			}

			// don't draw grid during cutscenes or quest events
			if (drawState == DrawState.HiddenCutscene || drawState == DrawState.Partially)
			{
				_hudModules.ForEach(module => module.Draw(drawState, origin));
				ImGui.End();
				return;
			}

			// draw grid
			if (_gridConfig is not null && _gridConfig.Enabled)
			{
				DraggablesHelper.DrawGrid(_gridConfig, _hudOptions, _selectedElement);
			}

			// draw modules
			_hudModules.ForEach(module => module.Draw(drawState, origin));

			// draw elements
			lock (_hudElements)
			{
				DraggablesHelper.DrawElements(origin, _hudHelper, _hudElements, _selectedElement);
			}

			// draw tooltip
			TooltipsHelper.Instance.Draw();

			ImGui.End();
		}

		private void AssignActors()
		{
			// player
			var player = Plugin.ClientState.LocalPlayer;
			foreach (var element in _hudElementsUsingPlayer)
			{
				element.Actor = player;
			}

			// target
			var target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
			foreach (var element in _hudElementsUsingTarget)
			{
				element.Actor = target;
			}

			// target of target
			var targetOfTarget = Utils.FindTargetOfTarget(target, player, Plugin.ObjectTable);
			foreach (var element in _hudElementsUsingTargetOfTarget)
			{
				element.Actor = targetOfTarget;
			}

			// focus
			var focusTarget = Plugin.TargetManager.FocusTarget;
			foreach (var element in _hudElementsUsingFocusTarget)
			{
				element.Actor = focusTarget;
			}
		}
	}

	internal static class HUDConstants
	{
		internal static int BaseHUDOffsetY = (int) (ImGui.GetMainViewport().Size.Y * 0.3f);
		internal static int UnitFramesOffsetX = 160;
		internal static int PlayerCastbarY = BaseHUDOffsetY - 13;
		internal static int JobHudsBaseY = PlayerCastbarY - 14;
		internal static Vector2 DefaultBigUnitFrameSize = new Vector2(270, 50);
		internal static Vector2 DefaultSmallUnitFrameSize = new Vector2(120, 20);
		internal static Vector2 DefaultStatusEffectsListSize = new Vector2(292, 82);
	}
}