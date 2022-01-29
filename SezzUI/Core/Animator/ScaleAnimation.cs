using System;

namespace SezzUI.Animator
{
	public enum ScalingType
	{
		Upscale,
		Downscale
	}

	public class ScaleAnimation : BaseAnimation
	{
		public float MinScale;
		public float MaxScale;
		private readonly ScalingType _defaultType;
		private readonly ScalingType _currentType; // TODO: Use for reversing animation?

		public ScaleAnimation(float fromScale = 0, float toScale = 1, uint duration = 0, uint delayStart = 0, uint delayEnd = 0)
		{
			_duration = (uint) Math.Max(50f, duration);
			_delayStart = delayStart;
			_delayEnd = delayEnd;

			MinScale = Math.Max(0.01f, Math.Min(fromScale, toScale));
			MaxScale = Math.Min(100f, Math.Max(fromScale, toScale));
			_defaultType = toScale > fromScale ? ScalingType.Upscale : ScalingType.Downscale;
			_currentType = _defaultType;
		}

		public override void Update()
		{
			if (_isPlaying && _ticksStart != null && Data != null)
			{
				int ticksNow = Environment.TickCount;
				int timeElapsed = ticksNow - (int) _ticksStart;

				if (timeElapsed > StartDelay && timeElapsed <= StartDelay + Duration)
				{
					int timeElapsedAnimating = timeElapsed - (int) StartDelay;

					float scaleFrom = _currentType == ScalingType.Upscale ? MinScale : MaxScale;
					float scaleTo = _currentType == ScalingType.Upscale ? MaxScale : MinScale;

					float scaleRange = scaleTo - scaleFrom;
					float scaleProgress = Math.Min(1, Math.Max(0, timeElapsedAnimating / (float) Duration));
					Data.Scale = scaleFrom + scaleRange * scaleProgress;
				}

				if (timeElapsed > StartDelay + Duration + EndDelay)
				{
					// Done
					_isPlaying = false;
				}
			}
		}
	}
}