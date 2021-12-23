using System;
using Dalamud.Logging;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.GameEvents
{
    internal class JobChangedEventArgs : EventArgs
    {
        public JobChangedEventArgs(uint jobId)
        {
            JobId = jobId;
        }

        public uint JobId { get; set; }
    }

    internal class LevelChangedEventArgs : EventArgs
    {
        public LevelChangedEventArgs(byte level)
        {
            Level = level;
        }

        public byte Level { get; set; }
    }

    internal sealed unsafe class Player : BaseGameEvent
    {
        public event EventHandler<JobChangedEventArgs>? JobChanged;
        public event EventHandler<LevelChangedEventArgs>? LevelChanged;
       
        private static readonly Lazy<Player> ev = new Lazy<Player>(() => new Player());
        public static Player Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        private uint lastJobId = 0;
        private byte lastLevel = 0;

        public override void Enable()
        {
            if (!Enabled)
            {
                PluginLog.Debug($"[Event:{Name}] Enable");
                Enabled = true;

                Plugin.Framework.Update += FrameworkUpdate;
            }
            else
            {
                PluginLog.Debug($"[Event:{Name}] Enable skipped");
            }
        }

        public override void Disable()
        {
            if (Enabled)
            {
                PluginLog.Debug($"[Event:{Name}] Disable");
                Enabled = false;

                Plugin.Framework.Update -= FrameworkUpdate;
            }
            else
            {
                PluginLog.Debug($"[Event:{Name}] Disable skipped");
            }
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
            try
            {
                PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
      
                // Job
                uint jobId = (player != null ? player.ClassJob.Id : 0);
                if (jobId != lastJobId)
                {
                    lastJobId = jobId;
                    PluginLog.Debug($"[{Name}::JobChanged] Job ID: {jobId}");
                    JobChanged?.Invoke(this, new JobChangedEventArgs(jobId));
                }

                // Level
                byte level = (player != null ? player.Level : (byte)0);
                if (level != lastLevel)
                {
                    lastLevel = level;
                    PluginLog.Debug($"[{Name}::LevelChanged] Level: {level}");
                    LevelChanged?.Invoke(this, new LevelChangedEventArgs(level));
                }
            }
            catch
            {
            }
        }
    }
}
