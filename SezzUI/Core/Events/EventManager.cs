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

        protected static PluginConfigObject _config = null!;
        public static DeveloperConfig Config => (DeveloperConfig)_config;

        #region Singleton
        public static void Initialize() { Instance = new EventManager(); }

        public EventManager()
        {
            _config = ConfigurationManager.Instance.GetConfigObject<DeveloperConfig>();
            ConfigurationManager.Instance.ResetEvent += OnConfigReset;
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
            
            ConfigurationManager.Instance.ResetEvent -= OnConfigReset;

            Instance = null!;
        }
        #endregion

        private void OnConfigReset(ConfigurationManager sender)
        {
            _config = sender.GetConfigObject<DeveloperConfig>();
        }
    }
}
