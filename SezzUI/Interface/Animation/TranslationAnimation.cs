using System;
using System.Numerics;

namespace SezzUI.Interface.Animation
{
	public class TranslationAnimation : BaseAnimation
	{
		public Vector2 OffsetFrom;
		public Vector2 OffsetTo;

		public TranslationAnimation(Vector2 from, Vector2 to, uint duration = 0, uint delayStart = 0, uint delayEnd = 0)
		{
			Duration = (uint) Math.Max(50f, duration);
			StartDelay = delayStart;
			EndDelay = delayEnd;

			OffsetFrom = from;
			OffsetTo = to;
		}

		public override void Update()
		{
			if (IsPlaying && TicksStart != null)
			{
				int ticksNow = Environment.TickCount;
				int timeElapsed = ticksNow - (int) TicksStart;

				if (timeElapsed > StartDelay && timeElapsed <= StartDelay + Duration)
				{
					int timeElapsedAnimating = timeElapsed - (int) StartDelay;

					Vector2 range = OffsetTo - OffsetFrom;
					float progress = Math.Min(1, Math.Max(0, timeElapsedAnimating / (float) Duration));
					if (Data != null)
					{
						Data.Offset = OffsetFrom + range * progress;
					}
				}

				if (timeElapsed > StartDelay + Duration + EndDelay)
				{
					// Done
					IsPlaying = false;
				}
			}
		}
	}
}