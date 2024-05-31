using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using SezzUI.Modules;

namespace SezzUI.Game.Events;

internal sealed class Player : BaseEvent
{
	public delegate void JobChangedDelegate(uint jobId);

	public event JobChangedDelegate? JobChanged;

	public delegate void LevelChangedDelegate(byte level);

	public event LevelChangedDelegate? LevelChanged;

	private uint _lastJobId;
	private byte _lastLevel;

	public Player()
	{
		(this as IPluginComponent).Enable();
	}

	protected override void OnEnable()
	{
		Services.Framework.Update += OnFrameworkUpdate;
	}

	protected override void OnDisable()
	{
		Services.Framework.Update -= OnFrameworkUpdate;
	}

	private void OnFrameworkUpdate(IFramework framework)
	{
		try
		{
			Update();
		}
		catch (Exception ex)
		{
			Logger.Error(ex);
		}
	}

	private void Update()
	{
		PlayerCharacter? player = Services.ClientState.LocalPlayer;

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
					Logger.Debug($"Job ID: {jobId}");
				}
#endif
				JobChanged?.Invoke(jobId);
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed invoking {nameof(JobChanged)}: {ex}");
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
					Logger.Debug($"Level: {level}");
				}
#endif
				LevelChanged?.Invoke(level);
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed invoking {nameof(LevelChanged)}: {ex}");
		}
	}
}