using System;
using System.Numerics;

namespace SezzUI.Interface.Animation;

public class AnimatorTimelines
{
	public Timeline OnShow;
	public Timeline Loop;
	public Timeline OnHide;

	public AnimatorTimelines(Animator animator)
	{
		OnShow = new(animator);
		OnShow.Data.DefaultOpacity = 1;

		Loop = new(animator);

		OnHide = new(animator);
		OnHide.Data.DefaultOpacity = 0;
	}
}

public class AnimatorTransformData
{
	public Vector2 Offset;
	public Vector4 Color;
	public float Opacity;
	public float Scale;

	public Vector2 DefaultOffset = new(0, 0);
	public Vector4 DefaultColor = new(1f, 1f, 1f, 1f);
	public float DefaultOpacity = 1f;
	public float DefaultScale = 1f;

	public void Reset()
	{
		Offset = DefaultOffset;
		Color = DefaultColor;
		Opacity = DefaultOpacity;
		Scale = DefaultScale;
	}

	public void Reset(AnimatorTransformData data)
	{
		Offset = data.DefaultOffset;
		Color = data.DefaultColor;
		Opacity = data.DefaultOpacity;
		Scale = data.DefaultScale;
	}

	public void SetDefaults(AnimatorTransformData data)
	{
		DefaultOffset = data.DefaultOffset;
		DefaultColor = data.DefaultColor;
		DefaultOpacity = data.DefaultOpacity;
		DefaultScale = data.DefaultScale;
	}

	public static AnimatorTransformData operator +(AnimatorTransformData data1, AnimatorTransformData data2)
	{
		AnimatorTransformData data = new()
		{
			Offset = data1.Offset + data2.Offset,
			Color = data2.Color,
			Opacity = data2.Opacity,
			Scale = data1.Scale * data2.Scale
		};
		return data;
	}
}

public class Animator : IDisposable
{
	public AnimatorTimelines Timelines;
	public AnimatorTransformData Data = new(); // This will be a reference to the active timeline's Data!

	/// <summary>
	///     Is playing OnShow, Loop or OnHide. This is TRUE even if Loop doesn't contain animations.
	///     You can use IsLooping to check if the animation is running a empty Loop timeline.
	/// </summary>
	public bool IsAnimating { get; private set; }

	public bool IsLooping { get; private set; }

	private int? _ticksStart;
	private int? _ticksStop;

	public int TimeElapsed => _ticksStart != null ? Environment.TickCount - (int) _ticksStart : 0;

	public Animator()
	{
		Timelines = new(this);
	}

	/// <summary>
	///     Updates animation values (positions/opacity/etc).
	/// </summary>
	/// <returns>IsAnimating</returns>
	public bool Update()
	{
		if (IsAnimating && _ticksStart != null)
		{
			int ticksNow = Environment.TickCount;

			if (_ticksStop == null)
			{
				int timeElapsed = ticksNow - (int) _ticksStart;
				if (timeElapsed <= Timelines.OnShow.Duration)
				{
					// OnShow
					Timelines.OnShow.Update();
				}
				else
				{
					// Loop
					if ((Timelines.Loop.HasAnimations && !Timelines.Loop.IsPlaying) || Timelines.Loop.Data != Data)
					{
						IsLooping = !Timelines.Loop.HasAnimations;
						Timelines.Loop.Play((int) _ticksStart + (int) Timelines.OnShow.Duration, true);
					}

					Timelines.Loop.Update();
				}
			}
			else
			{
				// OnHide
				IsLooping = false;
				int timeElapsed = ticksNow - (int) _ticksStop;
				if (timeElapsed <= Timelines.OnHide.Duration)
				{
					Timelines.OnHide.Update();
				}
				else
				{
					IsAnimating = false;
				}
			}
		}

		return IsAnimating;
	}

	public void SetData(ref AnimatorTransformData data)
	{
		Data = data;
	}

	public void Animate()
	{
		if (IsAnimating)
		{
			Stop(true);
		}

		if (!IsAnimating)
		{
			_ticksStart = Environment.TickCount;
			_ticksStop = null;
			IsAnimating = true;
			Timelines.OnShow.Data.Reset();
			Timelines.OnShow.Play((int) _ticksStart);
			Timelines.Loop.Data.Reset();
		}
	}

	public void Stop(bool force = false)
	{
		if (IsAnimating)
		{
			if (Timelines.OnShow.IsPlaying)
			{
				Timelines.OnShow.Stop();
			}

			if (Timelines.Loop.IsPlaying)
			{
				Timelines.Loop.Stop();
				IsLooping = false;
			}

			if (!force)
			{
				if (!Timelines.OnHide.IsPlaying)
				{
					_ticksStop = Environment.TickCount;
					Timelines.OnHide.Data.Reset();
					Timelines.OnHide.Play((int) _ticksStop);
				}
			}
			else
			{
				IsAnimating = false;
			}
		}
	}

	~Animator()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Stop(true);
	}
}