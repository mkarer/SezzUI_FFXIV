using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.GameEvents
{
	internal sealed class Player : BaseGameEvent
	{
		public delegate void JobChangedDelegate(uint jobId);

		public event JobChangedDelegate? JobChanged;

		public delegate void LevelChangedDelegate(byte level);

		public event LevelChangedDelegate? LevelChanged;

		private uint _lastJobId;
		private byte _lastLevel;

		#region Singleton

		private static readonly Lazy<Player> _ev = new(() => new());
		public static Player Instance => _ev.Value;
		public static bool Initialized => _ev.IsValueCreated;

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
				Logger.Error(ex, "OnFrameworkUpdate", $"Error: {ex}");
			}
		}

		private void Update()
		{
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;

			try
			{
				// Job
				uint jobId = player != null ? player.ClassJob.Id : 0;
				if (jobId != _lastJobId)
				{
					_lastJobId = jobId;
#if DEBUG
					if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventPlayerJobChanged)
					{
						Logger.Debug("JobChanged", $"Job ID: {jobId}");
					}
#endif
					JobChanged?.Invoke(jobId);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "JobChanged", $"Failed invoking {nameof(JobChanged)}: {ex}");
			}

			try
			{
				// Level
				byte level = player != null ? player.Level : (byte) 0;
				if (level != _lastLevel)
				{
					_lastLevel = level;
#if DEBUG
					if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventPlayerLevelChanged)
					{
						Logger.Debug("LevelChanged", $"Level: {level}");
					}
#endif
					LevelChanged?.Invoke(level);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "LevelChanged", $"Failed invoking {nameof(LevelChanged)}: {ex}");
			}
		}
	}
}