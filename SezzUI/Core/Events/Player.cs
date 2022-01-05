using System;
using Dalamud.Logging;
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
                Plugin.Framework.Update += FrameworkUpdate;
                return true;
            }
        
            return false;
        }

        public override bool Disable()
        {
            if (base.Disable())
            {
                Plugin.Framework.Update -= FrameworkUpdate;
                return true;
            }

            return false;
        }

        private void FrameworkUpdate(Framework framework)
        {
            try
            {
                Update();
            }
            catch
            {
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
                    PluginLog.Debug($"[Event:{GetType().Name}::JobChanged] Job ID: {jobId}");
                    JobChanged?.Invoke(jobId);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[Event:{GetType().Name}::JobChanged] Failed invoking {nameof(this.JobChanged)}: {ex}");
            }

            try
            {
                // Level
                byte level = (player != null ? player.Level : (byte)0);
                if (level != lastLevel)
                {
                    lastLevel = level;
                    PluginLog.Debug($"[Event:{GetType().Name}::LevelChanged] Level: {level}");
                    LevelChanged?.Invoke(level);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[Event:{GetType().Name}::LevelChanged] Failed invoking {nameof(this.LevelChanged)}: {ex}");
            }
        }
    }
}
