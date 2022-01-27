using System;
using System.Numerics;

namespace SezzUI.Core
{
    public abstract class AnimatedHudElement : IDisposable
    {
        public bool IsShown => _isShown;
		private bool _isShown = false;
        public Animator.Animator Animator = new();

        public virtual void Show()
        {
			if (!IsShown)
			{
                _isShown = !IsShown;
				Animator.Animate();
			}
		}

        public virtual void Hide(bool force = false)
        {
			if (IsShown)
			{
                _isShown = !IsShown;
				Animator.Stop(force);
			}
		}

		public virtual void Draw(Vector2 origin, int elapsed = 0)
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
