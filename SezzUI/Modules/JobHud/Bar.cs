using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SezzUI.Enums;
using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud
{
	public class Bar : IDisposable
	{
		public JobHud Parent { get; }

		private readonly List<Icon> _icons;
		public bool HasIcons => _icons.Count > 0;

		public Vector2 IconSize
		{
			get => _iconSize;
			set
			{
				_iconSize = value;
				(IconUV0, IconUV1) = DrawHelper.GetTexCoordinates(IconSize);
			}
		}

		public Vector2 IconUV0 = new(0, 0);
		public Vector2 IconUV1 = new(1, 1);
		public uint IconPadding = 8;
		private Vector2 _iconSize; // 36px Icon + 1px Borders

		public Vector2 Size = Vector2.Zero;

		public Bar(JobHud hud)
		{
			Parent = hud;
			_icons = new();
			IconSize = new(38, 38); // 36px Icon + 1px Borders
		}

		public void Add(Icon icon, int index = -1)
		{
			if (!icon.ShouldShow())
			{
				icon.Dispose();
				return;
			}

			if (icon.CooldownActionId != null)
			{
				EventManager.Cooldown.Watch((uint) icon.CooldownActionId);
			}

			if (index == -1)
			{
				_icons.Add(icon);
			}
			else
			{
				_icons.Insert(index, icon);
			}

			Size.Y = IconSize.Y;
			Size.X = IconSize.X * _icons.Count() + (_icons.Count() - 1) * IconPadding;
		}

		public void Draw(Vector2 anchor, Animator.Animator animator)
		{
			if (!HasIcons)
			{
				return;
			}

			Vector2 pos = DrawHelper.GetAnchoredPosition(anchor, Vector2.Zero, Size, DrawAnchor.Top);

			DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_JobHudBar", pos, Size, false, false, drawList =>
			{
				Vector2 iconPos = Vector2.Zero;
				iconPos.Y = pos.Y;

				for (int i = 0; i < _icons.Count; i++)
				{
					iconPos.X = pos.X + i * (IconPadding + IconSize.X);
					_icons[i].Draw(iconPos, IconSize, animator, drawList);
				}
			});
		}

		~Bar()
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

			_icons.ForEach(icon =>
			{
				if (icon.CooldownActionId != null)
				{
					EventManager.Cooldown.Unwatch((uint) icon.CooldownActionId);
				}

				icon.Dispose();
			});
		}
	}
}