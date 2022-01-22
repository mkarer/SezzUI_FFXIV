using System;
using SezzUI.Config;
using SezzUI.Interface.GeneralElements;

namespace SezzUI
{
    internal class EventManager : IDisposable
    {
        internal static GameEvents.Game Game { get { return GameEvents.Game.Instance; } }
        internal static GameEvents.Player Player { get { return GameEvents.Player.Instance; } }
        internal static GameEvents.Combat Combat { get { return GameEvents.Combat.Instance; } }
        internal static GameEvents.CombatLog CombatLog { get { return GameEvents.CombatLog.Instance; } }
        internal static GameEvents.Cooldown Cooldown { get { return GameEvents.Cooldown.Instance; } }

#if DEBUG
        protected static PluginConfigObject _config = null!;
        public static GeneralDebugConfig Config => (GeneralDebugConfig)_config;
#endif

#region Singleton
        public static void Initialize() { Instance = new EventManager(); }

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

            if (GameEvents.Game.Initialized) { Game.Dispose(); }
            if (GameEvents.Player.Initialized) { Player.Dispose(); }
            if (GameEvents.Combat.Initialized) { Combat.Dispose(); }
            if (GameEvents.Cooldown.Initialized) { Cooldown.Dispose(); }
            if (GameEvents.CombatLog.Initialized) { CombatLog.Dispose(); }

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
