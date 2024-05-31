using System;

namespace SezzUI.Interface.Animation;

public enum FadeDirection
{
	In,
	Out
}

public class FadeAnimation : BaseAnimation
{
	public float MinOpacity;
	public float MaxOpacity;
	private readonly FadeDirection _defaultDirection;
	private readonly FadeDirection _currentDirection; // TODO: Use for reversing animation?

	public FadeAnimation(float fromOpacity = 0, float toOpacity = 1, uint duration = 0, uint delayStart = 0, uint delayEnd = 0)
	{
		Duration = (uint) Math.Max(50f, duration);
		StartDelay = delayStart;
		EndDelay = delayEnd;

		MinOpacity = Math.Max(0f, Math.Min(fromOpacity, toOpacity));
		MaxOpacity = Math.Min(1f, Math.Max(fromOpacity, toOpacity));
		_defaultDirection = toOpacity > fromOpacity ? FadeDirection.In : FadeDirection.Out;
		_currentDirection = _defaultDirection;
	}

	public override void Update()
	{
		if (IsPlaying && TicksStart != null && Data != null)
		{
			int ticksNow = Environment.TickCount;
			int timeElapsed = ticksNow - (int) TicksStart;

			if (timeElapsed > StartDelay && timeElapsed <= StartDelay + Duration)
			{
				int timeElapsedAnimating = timeElapsed - (int) StartDelay;

				float fadeFrom = _currentDirection == FadeDirection.In ? MinOpacity : MaxOpacity;
				float fadeTo = _currentDirection == FadeDirection.In ? MaxOpacity : MinOpacity;

				float fadeRange = fadeTo - fadeFrom;
				float fadeProgress = Math.Min(1, Math.Max(0, timeElapsedAnimating / (float) Duration));
				Data.Opacity = fadeFrom + fadeRange * fadeProgress;
			}

			if (timeElapsed > StartDelay + Duration + EndDelay)
			{
				// Done
				IsPlaying = false;
			}
		}
	}
}