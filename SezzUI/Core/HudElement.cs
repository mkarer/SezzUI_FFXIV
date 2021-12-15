using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
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

        public virtual void Dispose()
        {
        }
    }
}
