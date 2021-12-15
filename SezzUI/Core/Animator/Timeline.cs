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

		public bool HasStartTime { get { return _ticksStart != null; } }
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

		public void Chain(BaseAnimation animation)
		{
			animation._delayStart = _duration;
			Add(animation);
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
			else if (!HasAnimations && HasStartTime)
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
					foreach (BaseAnimation animation in _animations)
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
