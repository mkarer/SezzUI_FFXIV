using System;

namespace SezzUI.Animator
{
	public class AnimatorTimelines
	{
		public Timeline OnShow;
		public Timeline Loop;
		public Timeline OnHide;

		public AnimatorTimelines(Animator animator) {
			OnShow = new Timeline(animator);
			Loop = new Timeline(animator);
			OnHide = new Timeline(animator);
		}
	}

	public class AnimatorTransformData
	{
		public float X;
		public float Y;
		public float Opacity;
		public float Scale;

		public float DefaultX = 0;
		public float DefaultY = 0;
		public float DefaultOpacity = 1f;
		public float DefaultScale = 1f;

		public void Reset()
		{
			X = DefaultX;
			Y = DefaultY;
			Opacity = DefaultOpacity;
			Scale = DefaultScale;
		}

		public void Reset(AnimatorTransformData data)
		{
			X = data.DefaultX;
			Y = data.DefaultY;
			Opacity = data.DefaultOpacity;
			Scale = data.DefaultScale;
		}

		public void SetDefaults(AnimatorTransformData data)
		{
			DefaultX = data.DefaultX;
			DefaultY = data.DefaultY;
			DefaultOpacity = data.DefaultOpacity;
			DefaultScale = data.DefaultScale;
		}

		public static AnimatorTransformData operator +(AnimatorTransformData data1, AnimatorTransformData data2)
		{
			AnimatorTransformData data = new AnimatorTransformData
			{
				X = data1.X + data2.X,
				Y = data1.Y + data2.Y,
				Opacity = data2.Opacity,
				Scale = data1.Scale * data2.Scale
			};
			return data;
		}
	}

	public class Animator : IDisposable
	{
		public AnimatorTimelines Timelines;
		public AnimatorTransformData Data = new AnimatorTransformData(); // This will be a reference to the active timeline's Data!
		public bool IsAnimating { get { return _isAnimating; } }

		private bool _isAnimating = false;
		private int? _ticksStart;
		private int? _ticksStop;

		public int TimeElapsed {
			get
			{
				return _ticksStart != null ? Environment.TickCount - (int)_ticksStart : 0;
			}
		}

		public Animator()
		{
			Timelines = new AnimatorTimelines(this);
		}

		public void Update()
		{
			if (_isAnimating && _ticksStart != null) {
				int ticksNow = Environment.TickCount;

				if (_ticksStop == null)
				{
					int timeElapsed = ticksNow - (int)_ticksStart;
					if (timeElapsed <= Timelines.OnShow.Duration)
					{
						// OnShow
						Timelines.OnShow.Update();
					}
					else
					{
						// Loop
						if (Timelines.Loop.HasAnimations && !Timelines.Loop.IsPlaying)
						{
							Timelines.Loop.Play((int)_ticksStart + (int)Timelines.OnShow.Duration, true);
						}
						Timelines.Loop.Update();
					}
				}
				else
				{
					// OnHide
					int timeElapsed = ticksNow - (int)_ticksStop;
					if (timeElapsed <= Timelines.OnHide.Duration)
					{
						Timelines.OnHide.Update();
					}
					else
					{
						_isAnimating = false;
					}
				}
			}
		}

		public void SetData(ref AnimatorTransformData data)
		{
			Data = data;
		}

		public void Animate()
		{
			if (_isAnimating)
			{
				Stop(true);
			}

			if (!_isAnimating)
			{
				_ticksStart = Environment.TickCount;
				_ticksStop = null;
				_isAnimating = true;
				Timelines.OnShow.Data.Reset();
				Timelines.OnShow.Play((int)_ticksStart);
				Timelines.Loop.Data.Reset();
			}
		}

		public void Stop(bool force = false)
		{
			if (_isAnimating) {
				if (Timelines.OnShow.IsPlaying)
					Timelines.OnShow.Stop();

				if (Timelines.Loop.IsPlaying)
					Timelines.Loop.Stop();

				if (!force)
				{
					if (!Timelines.OnHide.IsPlaying)
					{
						_ticksStop = Environment.TickCount;
						Timelines.OnHide.Data.Reset();
						Timelines.OnHide.Play((int)_ticksStop);
					}
				} else
				{
					_isAnimating = false;
				}
			}
		}

		public void Dispose()
		{
		}
	}
}
