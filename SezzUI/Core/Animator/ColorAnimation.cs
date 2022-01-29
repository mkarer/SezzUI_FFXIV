using System;
using System.Numerics;

namespace SezzUI.Animator
{
	public class ColorAnimation : BaseAnimation
	{
		public Vector4 ColorFrom;
		public Vector4 ColorTo;

		public ColorAnimation(Vector4 from, Vector4 to, uint duration = 0, uint delayStart = 0, uint delayEnd = 0)
		{
			_duration = (uint) Math.Max(50f, duration);
			_delayStart = delayStart;
			_delayEnd = delayEnd;

			ColorFrom = from;
			ColorTo = to;
		}

		public override void Update()
		{
			if (_isPlaying && _ticksStart != null)
			{
				int ticksNow = Environment.TickCount;
				int timeElapsed = ticksNow - (int) _ticksStart;

				if (timeElapsed > StartDelay && timeElapsed <= StartDelay + Duration)
				{
					int timeElapsedAnimating = timeElapsed - (int) StartDelay;

					Vector4 range = ColorTo - ColorFrom;
					float progress = Math.Min(1, Math.Max(0, timeElapsedAnimating / (float) Duration));
					if (Data != null)
					{
						Data.Color = ColorFrom + range * progress;
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