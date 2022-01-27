using System;
using SezzUI.Config;
using SezzUI.GameEvents;
using SezzUI.Interface.GeneralElements;

namespace SezzUI
{
	internal class EventManager : IDisposable
	{
		internal static Game Game => Game.Instance;
		internal static Player Player => Player.Instance;
		internal static Combat Combat => Combat.Instance;
		internal static Cooldown Cooldown => Cooldown.Instance;

#if DEBUG
		protected static PluginConfigObject _config = null!;
		public static GeneralDebugConfig Config => (GeneralDebugConfig) _config;
#endif

		#region Singleton

		public static void Initialize()
		{
			Instance = new();
		}

		public EventManager()
		{
#if DEBUG
			_config = ConfigurationManager.Instance.GetConfigObject<GeneralDebugConfig>();
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
#endif
		}

		public static EventManager Instance { get; private set; } = null!;

		~EventManager()
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

			if (Game.Initialized)
			{
				Game.Dispose();
			}

			if (Player.Initialized)
			{
				Player.Dispose();
			}

			if (Combat.Initialized)
			{
				Combat.Dispose();
			}

			if (Cooldown.Initialized)
			{
				Cooldown.Dispose();
			}

#if DEBUG
			ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
#endif

			Instance = null!;
		}

		#endregion

#if DEBUG
		private void OnConfigReset(ConfigurationManager sender)
		{
			_config = sender.GetConfigObject<GeneralDebugConfig>();
		}
#endif
	}
}