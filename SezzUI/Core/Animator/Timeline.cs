using System;
using System.Collections.Generic;

namespace SezzUI.Animator
{
	public class Timeline : IDisposable
	{
		private Animator _animator;
		public AnimatorTransformData Data;

		private List<BaseAnimation> _animations = new List<BaseAnimation>();

		public uint Duration { get { return _duration; } }
		private uint _duration = 0;
		internal bool _loop = false;

		public bool IsPlaying {
			get {
				if (_isPlaying && !_loop && _ticksStart != null && (int)_ticksStart + Duration < Environment.TickCount)
				{
					Stop();
				}

				return _isPlaying;
			}
		}

		private bool _isPlaying = false;
		private int? _ticksStart;

		public bool HasAnimations { get { return _hasAnimations; } }
		private bool _hasAnimations = false;

		public Timeline(Animator animator)
		{
			_animator = animator;
			Data = new AnimatorTransformData();
			Data.Reset();
		}

		public void Add(BaseAnimation animation)
		{
			animation.SetData(ref Data);
			_animations.Add(animation);
			_duration = Math.Max(_duration, animation.StartDelay + animation.Duration + animation.EndDelay);
			_hasAnimations = true;
		}

		public bool Update()
		{
			if (HasAnimations && _isPlaying)
			{
				_isPlaying = false;

				foreach (BaseAnimation animation in _animations)
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

			return false;
		}

		public void Play(int start, bool loop = false)
		{
			if (HasAnimations && !_isPlaying)
			{
				_ticksStart = start;
				_isPlaying = true;
				_loop = loop;
				_animator.SetData(ref Data);
				foreach (BaseAnimation animation in _animations)
				{
					animation.Play(start);
				}
			}
		}

		public void Stop()
		{
			if (_isPlaying)
			{
				_isPlaying = false;
				foreach (BaseAnimation animation in _animations)
				{
					animation.Stop();
				}
			}
		}

		public void Dispose()
		{
		}
	}
}
