using System;
using System.Numerics;

namespace SezzUI.Animator
{
	public class TranslationAnimation : BaseAnimation
	{
		public Vector2 OffsetFrom;
		public Vector2 OffsetTo;

		public TranslationAnimation(Vector2 from, Vector2 to, uint duration = 0, uint delayStart = 0, uint delayEnd = 0)
		{
			_duration = (uint)Math.Max(50f, duration);
			_delayStart = delayStart;
			_delayEnd = delayEnd;

			OffsetFrom = from;
			OffsetTo = to;
		}

		public override void Update()
		{
			if (_isPlaying && _ticksStart != null)
			{
				int ticksNow = Environment.TickCount;
				int timeElapsed = ticksNow - (int)_ticksStart;

				if (timeElapsed > StartDelay && timeElapsed <= StartDelay + Duration)
				{
					int timeElapsedAnimating = timeElapsed - (int)StartDelay;

					Vector2 range = OffsetTo - OffsetFrom;
					float progress = Math.Min(1, Math.Max(0, (float)timeElapsedAnimating / (float)Duration));
					if (Data != null)
					{
						Data.Offset = OffsetFrom + range * progress;
					}
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
