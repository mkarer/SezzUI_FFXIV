using System;
using System.Collections.Generic;

namespace SezzUI.Modules.CooldownHud
{
    public class CooldownHudItem : IDisposable
    {
        public uint ActionId;
        public ushort LastPulseCharges = 100;
        public List<BarManager.BarManager> barManagers = new();

        ~CooldownHudItem()
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

            barManagers.Clear();
        }
    }
}
