using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.GameEvents
{
    internal sealed unsafe class Player : BaseGameEvent
    {
        public delegate void JobChangedDelegate(uint jobId);
        public event JobChangedDelegate? JobChanged;
        
        public delegate void LevelChangedDelegate(byte level);
        public event LevelChangedDelegate? LevelChanged;
       
        private uint lastJobId = 0;
        private byte lastLevel = 0;

        #region Singleton
        private static readonly Lazy<Player> ev = new Lazy<Player>(() => new Player());
        public static Player Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }
        #endregion

        public override bool Enable()
        {
            if (base.Enable())
            {
                Plugin.Framework.Update += OnFrameworkUpdate;
                return true;
            }
        
            return false;
        }

        public override bool Disable()
        {
            if (base.Disable())
            {
                Plugin.Framework.Update -= OnFrameworkUpdate;
                return true;
            }

            return false;
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            try
            {
                Update();
            }
            catch (Exception ex)
            {
                LogError(ex, "OnFrameworkUpdate", $"Error: {ex}");
            }
        }

        private void Update()
        {
            PlayerCharacter? player = Plugin.ClientState.LocalPlayer;

            try
            {
                // Job
                uint jobId = (player != null ? player.ClassJob.Id : 0);
                if (jobId != lastJobId)
                {
                    lastJobId = jobId;
#if DEBUG
                    if (EventManager.Config.LogEvents && EventManager.Config.LogEventPlayerJobChanged)
                    {
                        LogDebug("JobChanged", $"Job ID: {jobId}");
                    }
#endif
                    JobChanged?.Invoke(jobId);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "JobChanged", $"Failed invoking {nameof(JobChanged)}: {ex}");
            }

            try
            {
                // Level
                byte level = (player != null ? player.Level : (byte)0);
                if (level != lastLevel)
                {
                    lastLevel = level;
#if DEBUG
                    if (EventManager.Config.LogEvents && EventManager.Config.LogEventPlayerLevelChanged)
                    {
                        LogDebug("LevelChanged", $"Level: {level}");
                    }
#endif
                    LevelChanged?.Invoke(level);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "LevelChanged", $"Failed invoking {nameof(LevelChanged)}: {ex}");
            }
        }
    }
}
