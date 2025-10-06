using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using SezzUI.Configuration;
using SezzUI.Enums;
using SezzUI.Helper;
using SezzUI.Interface.GeneralElements;
using SezzUI.Modules;
using SezzUI.Modules.CooldownHud;
using SezzUI.Modules.GameUI;
using SezzUI.Modules.JobHud;
using SezzUI.Modules.PluginMenu;
using SezzUI.Modules.ServerInfoBar;
using SezzUI.Modules.Tweaks;

namespace SezzUI.Interface;

public class HudManager : IPluginDisposable
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
	private static AutoDismount? _autoDismount; // TODO: Tweaks

	public HudManager()
	{
		ConfigurationManager configurationManager = Singletons.Get<ConfigurationManager>();
		configurationManager.ResetEvent += OnConfigReset;
		configurationManager.LockEvent += OnHUDLockChanged;
		configurationManager.ConfigClosedEvent += OnConfigWindowClosed;
	}

	private bool _isInitialized = false;
	
	public void Initialize()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;
			
			ConfigurationManager configurationManager = Singletons.Get<ConfigurationManager>();

			Singletons.Register(new JobHud(configurationManager.GetConfigObject<JobHudConfig>()));
			Singletons.Register(new CooldownHud(configurationManager.GetConfigObject<CooldownHudConfig>()));
			Singletons.Register(new ActionBar(configurationManager.GetConfigObject<ActionBarConfig>()));
			Singletons.Register(new ElementHider(configurationManager.GetConfigObject<ElementHiderConfig>()));
			Singletons.Register(new PluginMenu(configurationManager.GetConfigObject<PluginMenuConfig>()));
			Singletons.Register(new ServerInfoBar(configurationManager.GetConfigObject<ServerInfoBarConfig>()));
			Singletons.Register(new AutoDismount(configurationManager.GetConfigObject<AutoDismountConfig>()));

			CreateHudElements();
		}
	}

	bool IPluginDisposable.IsDisposed { get; set; } = false;

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
		if (!disposing || (this as IPluginDisposable).IsDisposed)
		{
			return;
		}

		_modules.ForEach(module => module.Dispose());
		_modules.Clear();
		_draggableHudElements.Clear();
		_hudElementsWithPreview.Clear();

		ConfigurationManager configurationManager = Singletons.Get<ConfigurationManager>();
		configurationManager.ResetEvent -= OnConfigReset;
		configurationManager.LockEvent -= OnHUDLockChanged;
		configurationManager.ConfigClosedEvent -= OnConfigWindowClosed;

		(this as IPluginDisposable).IsDisposed = true;
	}

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
		_gridConfig = Singletons.Get<ConfigurationManager>().GetConfigObject<GridConfig>();
		_hudOptions = Singletons.Get<ConfigurationManager>().GetConfigObject<HUDOptionsConfig>();

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
		_jobHud ??= Singletons.Get<JobHud>();
		_modules.Add(_jobHud);

		// Cooldown HUD
		_cooldownHud ??= Singletons.Get<CooldownHud>();
		_modules.Add(_cooldownHud);

		// Game UI Tweaks
		_actionBar ??= Singletons.Get<ActionBar>();
		_modules.Add(_actionBar);

		_elementHider ??= Singletons.Get<ElementHider>();
		_modules.Add(_elementHider);

		// Plugin Menu
		_pluginMenu ??= Singletons.Get<PluginMenu>();
		_modules.Add(_pluginMenu);

		// Server Info Bar
		_serverInfoBar ??= Singletons.Get<ServerInfoBar>();
		_modules.Add(_serverInfoBar);

		// Tweaks
		_autoDismount ??= Singletons.Get<AutoDismount>();
		_modules.Add(_autoDismount);
	}

	public void Draw(DrawState drawState)
	{
		Singletons.Get<TooltipsHelper>().RemoveTooltip(); // remove tooltip from previous frame

		// don't draw hud when it's not supposed to be visible
		if (drawState == DrawState.HiddenNotInGame || drawState == DrawState.HiddenDisabled)
		{
			_modules.Where(module => (module as IPluginComponent).IsEnabled).ToList().ForEach(module => module.Draw(drawState));
			return;
		}

		Singletons.Get<ClipRectsHelper>().Update();

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
			_modules.Where(module => (module as IPluginComponent).IsEnabled).ToList().ForEach(module => module.Draw(drawState));
			ImGui.End();
			return;
		}

		// draw grid
		if (_gridConfig is not null && _gridConfig.Enabled)
		{
			DraggablesHelper.DrawGrid(_gridConfig, _hudOptions, _selectedHudElement);
		}

		// draw modules
		_modules.Where(module => (module as IPluginComponent).IsEnabled).ToList().ForEach(module => module.Draw(drawState));

		// draw draggable elements
		if (!Singletons.Get<ConfigurationManager>().LockHUD)
		{
			lock (_draggableHudElements)
			{
				DraggablesHelper.DrawDraggableElements(_draggableHudElements, _selectedHudElement);
			}
		}

		// draw tooltip
		Singletons.Get<TooltipsHelper>().Draw();

		ImGui.End();
	}
}