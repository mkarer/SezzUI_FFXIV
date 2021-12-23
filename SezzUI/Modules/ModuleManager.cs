using System;

namespace SezzUI
{
    internal class ModuleManager : IDisposable
    {
        //internal static Modules.JobHud.JobHud JobHud { get { return Modules.JobHud.JobHud.Instance; } }

        #region Singleton
        public static void Initialize() { Instance = new ModuleManager(); }

        public static ModuleManager Instance { get; private set; } = null!;

        ~ModuleManager()
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

            //if (Modules.JobHud.JobHud.Initialized) { JobHud.Dispose(); }

            Instance = null!;
        }
        #endregion
    }
}
