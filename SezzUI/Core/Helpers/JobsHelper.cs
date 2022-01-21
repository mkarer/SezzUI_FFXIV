using System;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SezzUI.Helpers
{
	public static class JobsHelper
	{
		public enum PowerType
		{
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
			Lemure,
			Addersgall,
			Addersting,
			Kenki,
			MeditationStacks,
			Sen,
			Beast,
			Lily,
			BloodLily,
			PolyglotStacks,
			FirstmindsFocus,
			EyeOfTheDragon
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static unsafe byte GetUnsyncedLevel()
		{
			UIState* uiState = UIState.Instance();
			if (Plugin.ClientState.LocalPlayer == null || uiState == null || uiState->PlayerState.SyncedLevel == 0 || Plugin.ClientState.LocalPlayer?.ClassJob == null || Plugin.ClientState.LocalPlayer.ClassJob.GameData == null)
			{
				return Plugin.ClientState.LocalPlayer?.Level ?? 0;
			}

			int index = Plugin.ClientState.LocalPlayer.ClassJob.GameData.ExpArrayIndex & 0xff;
			return (byte) uiState->PlayerState.ClassJobLevelArray[index];
		}

		public static bool IsActionUnlocked(uint actionId)
		{
			LuminaAction? action = SpellHelper.GetAction(actionId);
			if (action == null)
			{
				return false;
			}

			byte jobLevel = action.IsRoleAction ? GetUnsyncedLevel() : Plugin.ClientState.LocalPlayer?.Level ?? 0;
			return action.ClassJobLevel <= jobLevel;
		}

		public static (int, int) GetPower(PowerType powerType)
		{
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
			byte jobLevel = player?.Level ?? 0;

			switch (powerType)
			{
				case PowerType.MP:
					return player != null ? ((int) player.CurrentMp, (int) player.MaxMp) : (0, 0);

				case PowerType.Oath:
					return jobLevel >= 35 ? (Plugin.JobGauges.Get<PLDGauge>().OathGauge, 100) : (0, 0);

				case PowerType.WhiteMana:
					return (Plugin.JobGauges.Get<RDMGauge>().WhiteMana, 100);

				case PowerType.BlackMana:
					return (Plugin.JobGauges.Get<RDMGauge>().BlackMana, 100);

				case PowerType.ManaStacks:
					return (Plugin.JobGauges.Get<RDMGauge>().ManaStacks, 3);

				case PowerType.Blood:
					return jobLevel >= 62 ? (Plugin.JobGauges.Get<DRKGauge>().Blood, 100) : (0, 0);

				case PowerType.Ammo:
					return jobLevel >= 30 ? (Plugin.JobGauges.Get<GNBGauge>().Ammo, jobLevel >= 88 ? 3 : 2) : (0, 0);

				case PowerType.Heat:
					return (Plugin.JobGauges.Get<MCHGauge>().Heat, 100);

				case PowerType.Battery:
					return (Plugin.JobGauges.Get<MCHGauge>().Battery, 100);

				case PowerType.Chakra:
					return (Plugin.JobGauges.Get<MNKGauge>().Chakra, 100);

				case PowerType.Ninki:
					return (Plugin.JobGauges.Get<NINGauge>().Ninki, 100);

				case PowerType.Soul:
					return (Plugin.JobGauges.Get<RPRGauge>().Soul, 100);

				case PowerType.Shroud:
					return (Plugin.JobGauges.Get<RPRGauge>().Shroud, 100);

				case PowerType.Lemure:
					return (Plugin.JobGauges.Get<RPRGauge>().LemureShroud, 5);

				case PowerType.Addersgall:
				{
					if (jobLevel < 45)
					{
						return (0, 0);
					}

					SGEGauge gauge = Plugin.JobGauges.Get<SGEGauge>();
					float adderScale = gauge.Addersgall + gauge.AddersgallTimer / 20000f; // 20s Addersgall Cooldown
					return ((int) Math.Floor(adderScale), 3);
				}

				case PowerType.Addersting:
					return jobLevel >= 66 ? (Plugin.JobGauges.Get<SGEGauge>().Addersting, 3) : (0, 0);

				case PowerType.Kenki:
					return (Plugin.JobGauges.Get<SAMGauge>().Kenki, 100);

				case PowerType.MeditationStacks:
					return (Plugin.JobGauges.Get<SAMGauge>().MeditationStacks, 100);

				case PowerType.Sen:
				{
					SAMGauge gauge = Plugin.JobGauges.Get<SAMGauge>();
					return (0 + (gauge.HasSetsu ? 1 : 0) + (gauge.HasGetsu ? 1 : 0) + (gauge.HasKa ? 1 : 0), 3);
				}

				case PowerType.Beast:
					return jobLevel >= 35 ? (Plugin.JobGauges.Get<WARGauge>().BeastGauge, 100) : (0, 0);

				case PowerType.Lily:
				{
					WHMGauge gauge = Plugin.JobGauges.Get<WHMGauge>();
					float lilyScale = gauge.Lily + gauge.LilyTimer / 30000f; // 30s Lily Cooldown
					return ((int) Math.Floor(lilyScale), 3);
				}

				case PowerType.BloodLily:
					return (Plugin.JobGauges.Get<WHMGauge>().BloodLily, 3);

				case PowerType.PolyglotStacks:
				{
					if (jobLevel < 70)
					{
						return (0, 0);
					}

					BLMGauge gauge = Plugin.JobGauges.Get<BLMGauge>();
					return (gauge.PolyglotStacks, jobLevel >= 80 ? 2 : 1);
				}

				case PowerType.FirstmindsFocus:
				{
					if (jobLevel < 90)
					{
						return (0, 0);
					}

					DRGGauge gauge = Plugin.JobGauges.Get<DRGGauge>();
					return (gauge.FirstmindsFocusCount, 2);
				}

				case PowerType.EyeOfTheDragon:
				{
					if (jobLevel < 60)
					{
						return (0, 0);
					}

					DRGGauge gauge = Plugin.JobGauges.Get<DRGGauge>();
					return (gauge.EyeCount, 2);
				}
				default:
					return (0, 0);
			}
		}
	}
}