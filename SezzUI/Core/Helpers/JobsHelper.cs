using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace SezzUI.Helpers
{
	public static class JobsHelper
	{
		public enum PowerType {
			Oath
		}

		public static (int, int) GetPower(PowerType ptype)
		{
			switch (ptype)
			{
				case PowerType.Oath:
					var gauge = Plugin.JobGauges.Get<PLDGauge>();
					return (gauge != null ? gauge.OathGauge : 0, 100);
			}

			return (0, 0);
		}
	}
}
