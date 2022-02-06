using System.Collections.Generic;
using SezzUI.Config;
using SezzUI.Interface.GeneralElements;
using SezzUI.Modules.ServerInfoBar.Entries;

namespace SezzUI.Modules.ServerInfoBar
{
	public class ServerInfoBar : PluginModule
	{
		private ServerInfoBarConfig Config => (ServerInfoBarConfig) _config;
#if DEBUG
		private readonly PluginMenuDebugConfig _debugConfig;
#endif

		private readonly List<Entry> _entries;

		internal override bool Enable()
		{
			if (!base.Enable())
			{
				return false;
			}

			_entries.ForEach(entry => entry.Toggle(entry.Config.Enabled));
			return true;
		}

		internal override bool Disable()
		{
			if (!base.Disable())
			{
				return false;
			}

			Logger.Debug("Disable entries");

			_entries.ForEach(entry => entry.Disable());
			return true;
		}

		#region Constructor

		public ServerInfoBar(PluginConfigObject config) : base(config)
		{
#if DEBUG
			_debugConfig = ConfigurationManager.Instance.GetConfigObject<PluginMenuDebugConfig>();
#endif
			Config.ValueChangeEvent += OnConfigPropertyChanged;
			ConfigurationManager.Instance.Reset += OnConfigReset;

			_entries = new()
			{
				new DutyFinderQueue(Config.DutyFinderQueueStatus)
			};

			Toggle(Config.Enabled);
		}

		#endregion

		#region Finalizer

		protected override void InternalDispose()
		{
			Disable();

			_entries.ForEach(entry => entry.Dispose());

			Config.ValueChangeEvent -= OnConfigPropertyChanged;
			ConfigurationManager.Instance.Reset -= OnConfigReset;
		}

		#endregion

		#region Singleton

		public static ServerInfoBar Initialize()
		{
			Instance = new(ConfigurationManager.Instance.GetConfigObject<ServerInfoBarConfig>());
			return Instance;
		}

		public static ServerInfoBar Instance { get; private set; } = null!;

		~ServerInfoBar()
		{
			Dispose(false);
		}

		#endregion

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			switch (args.PropertyName)
			{
				case "Enabled":
#if DEBUG
					if (_debugConfig.LogConfigurationManager)
					{
						Logger.Debug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.Enabled}");
					}
#endif
					Toggle(Config.Enabled);
					break;
			}
		}

		private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
		{
			if (config is not ServerInfoBarConfig)
			{
				return;
			}

#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("OnConfigReset", "Resetting...");
			}
#endif
			Disable();
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("OnConfigReset", $"Config.Enabled: {Config.Enabled}");
			}
#endif
			Toggle(Config.Enabled);
		}

		#endregion
	}
}