using System;
using System.Collections.Generic;
using SezzUI.Interface.BarManager;

namespace SezzUI.Modules.CooldownHud
{
	public class CooldownHudItem : IDisposable
	{
		public readonly List<BarManager> BarManagers = new();
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