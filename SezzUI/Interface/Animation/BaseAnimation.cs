using System;

namespace SezzUI.Interface.Animation;

public abstract class BaseAnimation : IDisposable
{
	public AnimatorTransformData? Data;

	public uint Duration { get; internal set; }
	public uint StartDelay { get; internal set; }
	public uint EndDelay { get; internal set; }
	public bool IsPlaying { get; protected set; }
	protected int? TicksStart { get; set; }

	public virtual void Update()
	{
	}

	public void Dispose()
	{
		Stop();
	}

	public void SetData(ref AnimatorTransformData data)
	{
		Data = data;
	}

	public virtual void Play(int start)
	{
		if (!IsPlaying && Data != null)
		{
			TicksStart = start;
			IsPlaying = true;
		}
	}

	public void Stop()
	{
		if (IsPlaying)
		{
			TicksStart = null;
			IsPlaying = false;
		}
	}
}