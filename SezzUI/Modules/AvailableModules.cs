using System;

namespace SezzUI
{
    public class AvailableModules : IDisposable
    {
        public Modules.JobHud.JobHud JobHud { get { return Modules.JobHud.JobHud.Instance; } }

        public void Dispose()
        {
            if (Modules.JobHud.JobHud.Initialized) { JobHud.Dispose(); }
        }
    }
}
