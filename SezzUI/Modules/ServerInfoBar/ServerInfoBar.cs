using System.Collections.Generic;
using SezzUI.Configuration;
using SezzUI.Helper;
using SezzUI.Modules.PluginMenu;
using SezzUI.Modules.ServerInfoBar.Entries;

namespace SezzUI.Modules.ServerInfoBar;

public class ServerInfoBar : PluginModule
{
	private ServerInfoBarConfig Config => (ServerInfoBarConfig) _config;
#if DEBUG
	private readonly PluginMenuDebugConfig _debugConfig;
#endif

	private readonly List<Entry> _entries;

	protected override void OnEnable()
	{
		_entries.ForEach(entry => (entry as IPluginComponent).SetEnabledState(entry.Config.Enabled));
	}

	protected override void OnDisable()
	{
		_entries.ForEach(entry => (entry as IPluginComponent).Disable());
	}

	public ServerInfoBar(PluginConfigObject config) : base(config)
	{
#if DEBUG
		_debugConfig = Singletons.Get<ConfigurationManager>().GetConfigObject<PluginMenuDebugConfig>();
#endif
		Config.ValueChangeEvent += OnConfigPropertyChanged;
		Singletons.Get<ConfigurationManager>().Reset += OnConfigReset;

		_entries = new()
		{
			new DutyFinderQueue(Config.DutyFinderQueueStatus)
		};

		(this as IPluginComponent).SetEnabledState(Config.Enabled);
	}

	protected override void OnDispose()
	{
		_entries.ForEach(entry => entry.Dispose());

		Config.ValueChangeEvent -= OnConfigPropertyChanged;
		Singletons.Get<ConfigurationManager>().Reset -= OnConfigReset;
	}

	~ServerInfoBar()
	{
		Dispose(false);
	}

	#region Configuration Events

	private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
	{
		switch (args.PropertyName)
		{
			case "Enabled":
#if DEBUG
				if (_debugConfig.LogConfigurationManager)
				{
					Logger.Debug($"{args.PropertyName}: {Config.Enabled}");
				}
#endif
				(this as IPluginComponent).SetEnabledState(Config.Enabled);
				break;
		}
	}

	private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
	{
		if (config != _config)
		{
			return;
		}

#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug("Resetting...");
		}
#endif
		(this as IPluginComponent).Disable();
#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug($"Config.Enabled: {Config.Enabled}");
		}
#endif
		(this as IPluginComponent).SetEnabledState(Config.Enabled);
	}

	#endregion
}