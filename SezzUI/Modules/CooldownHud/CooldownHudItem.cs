using System;
using System.Collections.Generic;

namespace SezzUI.Modules.CooldownHud
{
	public class CooldownHudItem : IDisposable
	{
		public readonly List<BarManager.BarManager> BarManagers = new();
		public uint ActionId;
		public ushort LastPulseCharges = 100;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~CooldownHudItem()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			BarManagers.Clear();
		}
	}
}