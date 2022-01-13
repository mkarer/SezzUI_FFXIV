using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SezzUI.BarManager;

namespace SezzUI.Modules.CooldownHud
{
    public class CooldownHudItem : IDisposable
    {
        public uint actionId;
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
