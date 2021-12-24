using System;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.Helpers
{
	public static class JobsHelper
	{
		public enum PowerType {
            MP,
			Oath,
			WhiteMana,
			BlackMana,
			ManaStacks,
			Blood,
			Ammo,
			Heat,
			Battery,
			Chakra,
			Ninki,
			Soul,
			Shroud,
			Addersgall,
			Addersting,
			Kenki,
			MeditationStacks,
			Beast,
			Lily,
			BloodLily
		}

		public static (int, int) GetPower(PowerType ptype)
		{
            PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
			byte jobLevel = player?.Level ?? 0;

			switch (ptype)
			{
                case PowerType.MP:
                    if (player != null)
                    {
                        return ((int)player.CurrentMp, (int)player.MaxMp);
                    }
                    return (0, 0);

				case PowerType.Oath:
					if (jobLevel >= 35)
					{
						return (Plugin.JobGauges.Get<PLDGauge>()?.OathGauge ?? 0, 100);
					}
					return (0, 0);

				case PowerType.WhiteMana:
					return (Plugin.JobGauges.Get<RDMGauge>()?.WhiteMana ?? 0, 100);

				case PowerType.BlackMana:
					return (Plugin.JobGauges.Get<RDMGauge>()?.BlackMana ?? 0, 100);

				case PowerType.ManaStacks:
					return (Plugin.JobGauges.Get<RDMGauge>()?.ManaStacks ?? 0, 3);

				case PowerType.Blood:
					if (jobLevel >= 62)
					{
						return (Plugin.JobGauges.Get<DRKGauge>()?.Blood ?? 0, 100);
					}
					return (0, 0);

				case PowerType.Ammo:
					if (jobLevel >= 30)
					{
						return (Plugin.JobGauges.Get<GNBGauge>()?.Ammo ?? 0, jobLevel >= 88 ? 3 : 2);
					}
					return (0, 0);

				case PowerType.Heat:
					return (Plugin.JobGauges.Get<MCHGauge>()?.Heat ?? 0, 100);

				case PowerType.Battery:
					return (Plugin.JobGauges.Get<MCHGauge>()?.Battery ?? 0, 100);

				case PowerType.Chakra:
					return (Plugin.JobGauges.Get<MNKGauge>()?.Chakra ?? 0, 100);

				case PowerType.Ninki:
					return (Plugin.JobGauges.Get<NINGauge>()?.Ninki ?? 0, 100);

				case PowerType.Soul:
					return (Plugin.JobGauges.Get<RPRGauge>()?.Soul ?? 0, 100);

				case PowerType.Shroud:
					return (Plugin.JobGauges.Get<RPRGauge>()?.Shroud ?? 0, 100);

				case PowerType.Addersgall:
					if (jobLevel >= 45)
					{
						SGEGauge gauge1 = Plugin.JobGauges.Get<SGEGauge>();
						if (gauge1 != null)
						{
							const float addersgallCooldown = 20000f;
							float GetScale(int num, float timer) => num + (timer / addersgallCooldown);
							float adderScale = GetScale(gauge1.Addersgall, gauge1.AddersgallTimer);
							return ((int)Math.Floor(adderScale), 3);
						}
					}
					return (0, 0);

				case PowerType.Addersting:
					if (jobLevel >= 66)
					{
						return (Plugin.JobGauges.Get<SGEGauge>()?.Addersting ?? 0, 3);

					}
					return (0, 0);

				case PowerType.Kenki:
					return (Plugin.JobGauges.Get<SAMGauge>()?.Kenki ?? 0, 100);

				case PowerType.MeditationStacks:
					return (Plugin.JobGauges.Get<SAMGauge>()?.MeditationStacks ?? 0, 100);

				case PowerType.Beast:
					if (jobLevel >= 35)
					{
						return (Plugin.JobGauges.Get<WARGauge>()?.BeastGauge ?? 0, 100);
					}
					return (0, 0);

				case PowerType.Lily:
					WHMGauge gauge2 = Plugin.JobGauges.Get<WHMGauge>();
					if (gauge2 != null)
					{
						const float lilyCooldown = 30000f;
						float GetScale(int num, float timer) => num + (timer / lilyCooldown);
						float lilyScale = GetScale(gauge2.Lily, gauge2.LilyTimer);
						return ((int)Math.Floor(lilyScale), 3);
					}
					return (0, 0);

				case PowerType.BloodLily:
					return (Plugin.JobGauges.Get<WHMGauge>()?.BloodLily ?? 0, 3);
			}

			return (0, 0);
		}
	}
}
