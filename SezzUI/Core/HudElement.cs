using System;
using System.Numerics;

namespace SezzUI.Core
{
    public abstract class AnimatedHudElement : IDisposable
    {
        public bool IsShown { get { return _isShown; } }
        internal bool _isShown = false;
        public Animator.Animator Animator = new();

        public virtual void Show()
        {
			if (!IsShown)
			{
                _isShown = !IsShown;
				Animator.Animate();
			}
		}

        public virtual void Hide()
        {
			if (IsShown)
			{
                _isShown = !IsShown;
				Animator.Stop();
			}
		}

		public virtual void Draw(Vector2 origin)
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
