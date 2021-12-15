using System;

namespace SezzUI
{
    public class AvailableEvents : IDisposable
    {
        public GameEvents.Player Player { get { return GameEvents.Player.Instance; } }
        public GameEvents.Combat Combat { get { return GameEvents.Combat.Instance; } }

        public void Dispose()
        {
            if (GameEvents.Player.Initialized) { Player.Dispose(); }
            if (GameEvents.Combat.Initialized) { Combat.Dispose(); }
        }
    }
}
