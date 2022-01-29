using System;

namespace SezzUI.Animator
{
	public abstract class BaseAnimation : IDisposable
	{
		public AnimatorTransformData? Data;

		public uint Duration => _duration;
		public uint StartDelay => _delayStart;
		public uint EndDelay => _delayEnd;
		public bool IsPlaying => _isPlaying;

		internal uint _duration = 0;
		internal uint _delayStart = 0;
		internal uint _delayEnd = 0;
		internal bool _isPlaying;
		internal int? _ticksStart;

		public virtual void Update()
		{
		}

		public void Dispose()
		{
			Stop();
		}

		public void SetData(ref AnimatorTransformData data)
		{
			Data = data;
		}

		public virtual void Play(int start)
		{
			if (!_isPlaying && Data != null)
			{
				_ticksStart = start;
				_isPlaying = true;
			}
		}

		public void Stop()
		{
			if (_isPlaying)
			{
				_ticksStart = null;
				_isPlaying = false;
			}
		}
	}
}