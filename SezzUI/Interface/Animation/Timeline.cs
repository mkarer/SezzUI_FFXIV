using System;
using System.Collections.Generic;

namespace SezzUI.Interface.Animation
{
	public class Timeline : IDisposable
	{
		private readonly Animator _animator;
		public AnimatorTransformData Data;

		public List<BaseAnimation> Animations { get; } = new();

		public uint Duration { get; private set; }

		internal bool _loop;

		public bool IsPlaying
		{
			get
			{
				if (_isPlaying && !_loop && _ticksStart != null && (int) _ticksStart + Duration < Environment.TickCount)
				{
					Stop();
				}

				return _isPlaying;
			}
		}

		public bool HasStartTime => _ticksStart != null;
		private bool _isPlaying;
		private int? _ticksStart;

		public bool HasAnimations { get; private set; }

		public Timeline(Animator animator)
		{
			_animator = animator;
			Data = new();
			Data.Reset();
		}

		public void Add(BaseAnimation animation)
		{
			animation.SetData(ref Data);
			Animations.Add(animation);
			Duration = Math.Max(Duration, animation.StartDelay + animation.Duration + animation.EndDelay);
			HasAnimations = true;
		}

		public void Chain(BaseAnimation animation)
		{
			animation.StartDelay = Duration;
			Add(animation);
		}

		public bool Update()
		{
			if (HasAnimations && _isPlaying)
			{
				_isPlaying = false;

				foreach (BaseAnimation animation in Animations)
				{
					animation.Update();
					_isPlaying |= animation.IsPlaying;
				}

				if (!_isPlaying && _loop)
				{
					Play(Environment.TickCount, _loop);
				}

				return true;
			}

			if (!HasAnimations && HasStartTime)
			{
				// Idle animation, just reset data to timeline's defaults...
				Data.Reset();
				_ticksStart = null;
			}

			return false;
		}

		public void Play(int start, bool loop = false)
		{
			if (!_isPlaying)
			{
				_animator.SetData(ref Data);
				_ticksStart = start;

				if (HasAnimations)
				{
					_isPlaying = true;
					_loop = loop;
					foreach (BaseAnimation animation in Animations)
					{
						animation.Play(start);
					}
				}
			}
		}

		public void Stop()
		{
			if (_isPlaying)
			{
				_isPlaying = false;
				foreach (BaseAnimation animation in Animations)
				{
					animation.Stop();
				}
			}
		}

		public void Dispose()
		{
			Stop();
		}
	}
}