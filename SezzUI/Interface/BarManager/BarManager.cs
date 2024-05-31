/*
* LibSezzTimerBars-4
* Initial port with basic functionality...
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Internal;
using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Interface.BarManager;

public class BarManager : IDisposable
{
	public BarDirection GrowDirection = BarDirection.Down;
	public DrawAnchor Anchor = DrawAnchor.TopLeft;
	public Vector2 Position = Vector2.Zero;

	public BarManagerBarConfig BarConfig = new();
	public readonly List<BarManagerBar> Bars = new();
	public int Count => Bars.Count();

	public readonly string Id;

	public BarManager(string id = "")
	{
		Id = id != "" ? id : Guid.NewGuid().ToString();
	}

	public bool Add(uint id, string? text, string? text2, IDalamudTextureWrap? icon, long start, uint duration, object? data = null, bool allowUpdating = true)
	{
		if (!allowUpdating && Get(id) != null)
		{
			return false;
		}

		return Update(id, text, text2, icon, start, duration, data);
	}

	public bool Update(uint id, string? text, string? text2, IDalamudTextureWrap? icon, long start, uint duration, object? data = null, bool allowAdding = true)
	{
		BarManagerBar? bar = Get(id);

		if (bar == null && allowAdding)
		{
			bar = new(this);
			bar.Id = id;
			Bars.Add(bar);
		}

		if (bar != null)
		{
			return Update(bar, text, text2, icon, start, duration, data);
		}

		return false;
	}

	public bool Update(BarManagerBar bar, string? text, string? text2, IDalamudTextureWrap? icon, long start, uint duration, object? data = null)
	{
		bar.Text = text;
		bar.CountText = text2;
		bar.Icon = icon;
		bar.StartTime = start;
		bar.Duration = duration;
		bar.Data = data;

		return true;
	}

	public bool Remove(uint id)
	{
		BarManagerBar? bar = Get(id);

		if (bar != null)
		{
			Bars.Remove(bar);
			bar.Dispose();
			return true;
		}

		return false;
	}

	public void RemoveExpired()
	{
		for (int i = Bars.Count - 1; i >= 0; i--)
		{
			if (!Bars[i].IsActive)
			{
				Bars.RemoveAt(i);
			}
		}
	}

	public void Clear()
	{
		Bars.ForEach(bar => bar.Dispose());
		Bars.Clear();
	}

	public BarManagerBar? Get(uint id) => Bars.Where(bar => bar.Id == id).FirstOrDefault();

	public void Draw()
	{
		if (!Bars.Any())
		{
			return;
		}

		RemoveExpired();

		Vector2 barPosition = DrawHelper.GetAnchoredPosition(Vector2.Zero, Anchor) + Position;
		Vector2 offset = Vector2.Zero;

		Bars.ForEach(bar =>
		{
			if (bar.IsActive)
			{
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