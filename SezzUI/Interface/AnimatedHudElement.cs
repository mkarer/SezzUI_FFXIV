using System;
using SezzUI.Interface.Animation;

namespace SezzUI.Interface
{
	public abstract class AnimatedHudElement : IDisposable
	{
		public bool IsShown { get; private set; }

		public Animator Animator = new();

		public virtual void Show()
		{
			if (!IsShown)
			{
				IsShown = !IsShown;
				Animator.Animate();
			}
		}

		public virtual void Hide(bool force = false)
		{
			if (IsShown)
			{
				IsShown = !IsShown;
				Animator.Stop(force);
			}
		}

		public virtual void Draw(int elapsed = 0)
		{
		}

		~AnimatedHudElement()
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

			Animator.Dispose();
			InternalDispose();
		}

		protected virtual void InternalDispose()
		{
			// override
		}
	}
}