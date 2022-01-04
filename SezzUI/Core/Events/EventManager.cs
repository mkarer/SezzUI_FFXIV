using System;

namespace SezzUI
{
    internal class EventManager : IDisposable
    {
        internal static GameEvents.Game Game { get { return GameEvents.Game.Instance; } }
        internal static GameEvents.Player Player { get { return GameEvents.Player.Instance; } }
        internal static GameEvents.Combat Combat { get { return GameEvents.Combat.Instance; } }

        #region Singleton
        public static void Initialize() { Instance = new EventManager(); }

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

            Instance = null!;
        }
        #endregion
    }
}
