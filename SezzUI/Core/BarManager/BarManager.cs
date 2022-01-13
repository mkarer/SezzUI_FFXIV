/*
 * LibSezzTimerBars-4
 * Initial port with basic functionality...
 */
using System;
using System.Collections.Generic;
using System.Linq;
using SezzUI.Interface.Bars;
using System.Numerics;
using SezzUI.Enums;
using ImGuiScene;

namespace SezzUI.BarManager
{
    public class BarManager : IDisposable
    {
        public BarDirection GrowDirection = BarDirection.Down;
        public DrawAnchor Anchor = DrawAnchor.TopLeft;
        public Vector2 Position = Vector2.Zero;

        public BarManagerBarConfig BarConfig = new();
        private List<BarManagerBar> _bars = new();
        public int Count => _bars.Count();

        public readonly string Id;

        public BarManager(string id = "")
        {
            Id = id != "" ? id : Guid.NewGuid().ToString();
        }

        public bool Add(uint id, string? text, TextureWrap? icon, long start, uint duration, bool allowUpdating = true)
        {
            if (!allowUpdating && Get(id) != null) {
                return false;
            }
            else {
                return Update(id, text, icon, start, duration, true);
            }
        }

        public bool Update(uint id, string? text, TextureWrap? icon, long start, uint duration, bool allowAdding = true)
        {
            BarManagerBar? bar = Get(id);

            if (bar == null && allowAdding)
            { 
                bar = new(this);
                bar.Id = id;
                _bars.Add(bar);
            }

            if (bar != null)
            {
                return Update(bar, text, icon, start, duration);
            }

            return false;
        }

        public bool Update(BarManagerBar bar, string? text, TextureWrap? icon, long start, uint duration)
        {
            bar.Text = text;
            bar.Icon = icon;
            bar.StartTime = start;
            bar.Duration = duration;

            return true;
        }

        public bool Remove(uint id)
        {
            BarManagerBar? bar = Get(id);

            if (bar != null)
            {
                _bars.Remove(bar);
                bar.Dispose();
                return true;
            }

            return false;
        }

        public void RemoveExpired()
        {
            for (int i = _bars.Count - 1; i >= 0; i--)
            {
                if (!_bars[i].IsActive) {
                    _bars.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _bars.ForEach(bar => bar.Dispose());
            _bars.Clear();
        }

        public BarManagerBar? Get(uint id) => _bars.Where(bar => bar.Id == id).FirstOrDefault();

        public void Draw(Vector2 origin)
        {
            if (!_bars.Any()) { return; }

            RemoveExpired();
       
            Vector2 barPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(Position, BarConfig.Size, Anchor);
            Vector2 offset = Vector2.Zero;

            _bars.ForEach(bar =>
            {
                if (bar.IsActive) {
                    bar.Draw(barPosition + offset);

                    switch (GrowDirection)
                    {
                        case BarDirection.Up:
                            offset.Y = offset.Y + bar.Config.Size.Y + bar.Config.BorderSize + bar.Config.Padding;
                            break;

                        case BarDirection.Right:
                            offset.X = offset.X + bar.Config.Size.X + bar.Config.BorderSize + bar.Config.Padding;
                            break;

                        case BarDirection.Down:
                            offset.Y = offset.Y - bar.Config.Size.Y - bar.Config.BorderSize - bar.Config.Padding;
                            break;

                        case BarDirection.Left:
                            offset.X = offset.X - bar.Config.Size.X - bar.Config.BorderSize - bar.Config.Padding;
                            break;
                    }
                }
            });
        }

        #region Destructor
        ~BarManager()
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

            Clear();
        }
        #endregion
    }
}
