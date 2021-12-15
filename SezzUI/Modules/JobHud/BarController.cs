using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace SezzUI.Modules.JobHud
{
    class BarController : IDisposable
    {
        private List<Bar> _bars = new();

        public void Reset()
        {

        }

        public void Draw(Vector2 origin)
        {
            _bars.ForEach(bar => bar.Draw(origin));
        }

        public void Dispose()
        {
            _bars.ForEach(bar => bar.Dispose());
            _bars.Clear();
        }
    }
}
