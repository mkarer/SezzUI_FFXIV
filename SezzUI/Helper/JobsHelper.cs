using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SezzUI.Enums;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SezzUI.Helper
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
			EyeOfTheDragon,
			Aetherflow
		}

		public static unsafe byte GetUnsyncedLevel()
		{
			UIState* uiState = UIState.Instance();
			if (Service.ClientState.LocalPlayer == null || uiState == null || uiState->PlayerState.SyncedLevel == 0 || Service.ClientState.LocalPlayer?.ClassJob == null || Service.ClientState.LocalPlayer.ClassJob.GameData == null)
			{
				return Service.ClientState.LocalPlayer?.Level ?? 0;
			}

			int index = Service.ClientState.LocalPlayer.ClassJob.GameData.ExpArrayIndex & 0xff;
			return (byte) uiState->PlayerState.ClassJobLevelArray[index];
		}

		public static bool IsActionUnlocked(uint actionId)
		{
			LuminaAction? action = SpellHelper.GetAction(actionId);
			if (action == null)
			{
				return false;
			}

			byte jobLevel = action.IsRoleAction ? GetUnsyncedLevel() : Service.ClientState.LocalPlayer?.Level ?? 0;
			return action.ClassJobLevel <= jobLevel;
		}

		public static (int, int) GetPower(PowerType powerType)
		{
			PlayerCharacter? player = Service.ClientState.LocalPlayer;
			byte jobLevel = player?.Level ?? 0;

			switch (powerType)
			{
				case PowerType.MP:
					return player != null ? ((int) player.CurrentMp, (int) player.MaxMp) : (0, 0);

				case PowerType.Oath:
					return jobLevel >= 35 ? (Service.JobGauges.Get<PLDGauge>().OathGauge, 100) : (0, 0);

				case PowerType.WhiteMana:
					return (Service.JobGauges.Get<RDMGauge>().WhiteMana, 100);

				case PowerType.BlackMana:
					return (Service.JobGauges.Get<RDMGauge>().BlackMana, 100);

				case PowerType.ManaStacks:
					return (Service.JobGauges.Get<RDMGauge>().ManaStacks, 3);

				case PowerType.Blood:
					return jobLevel >= 62 ? (Service.JobGauges.Get<DRKGauge>().Blood, 100) : (0, 0);

				case PowerType.Ammo:
					return jobLevel >= 30 ? (Service.JobGauges.Get<GNBGauge>().Ammo, jobLevel >= 88 ? 3 : 2) : (0, 0);

				case PowerType.Heat:
					return (Service.JobGauges.Get<MCHGauge>().Heat, 100);

				case PowerType.Battery:
					return (Service.JobGauges.Get<MCHGauge>().Battery, 100);

				case PowerType.Chakra:
					return (Service.JobGauges.Get<MNKGauge>().Chakra, 100);

				case PowerType.Ninki:
					return (Service.JobGauges.Get<NINGauge>().Ninki, 100);

				case PowerType.Soul:
					return (Service.JobGauges.Get<RPRGauge>().Soul, 100);

				case PowerType.Shroud:
					return (Service.JobGauges.Get<RPRGauge>().Shroud, 100);

				case PowerType.Lemure:
					return (Service.JobGauges.Get<RPRGauge>().LemureShroud, 5);

				case PowerType.Addersgall:
				{
					if (jobLevel < 45)
					{
						return (0, 0);
					}

					SGEGauge gauge = Service.JobGauges.Get<SGEGauge>();
					float adderScale = gauge.Addersgall + gauge.AddersgallTimer / 20000f; // 20s Addersgall Cooldown
					return ((int) Math.Floor(adderScale), 3);
				}

				case PowerType.Addersting:
					return jobLevel >= 66 ? (Service.JobGauges.Get<SGEGauge>().Addersting, 3) : (0, 0);

				case PowerType.Kenki:
					return (Service.JobGauges.Get<SAMGauge>().Kenki, 100);

				case PowerType.MeditationStacks:
					return (Service.JobGauges.Get<SAMGauge>().MeditationStacks, 100);

				case PowerType.Sen:
				{
					SAMGauge gauge = Service.JobGauges.Get<SAMGauge>();
					return (0 + (gauge.HasSetsu ? 1 : 0) + (gauge.HasGetsu ? 1 : 0) + (gauge.HasKa ? 1 : 0), 3);
				}

				case PowerType.Beast:
					return jobLevel >= 35 ? (Service.JobGauges.Get<WARGauge>().BeastGauge, 100) : (0, 0);

				case PowerType.Lily:
				{
					WHMGauge gauge = Service.JobGauges.Get<WHMGauge>();
					float lilyScale = gauge.Lily + gauge.LilyTimer / 30000f; // 30s Lily Cooldown
					return ((int) Math.Floor(lilyScale), 3);
				}

				case PowerType.BloodLily:
					return (Service.JobGauges.Get<WHMGauge>().BloodLily, 3);

				case PowerType.PolyglotStacks:
					if (jobLevel < 70)
					{
						return (0, 0);
					}

					return (Service.JobGauges.Get<BLMGauge>().PolyglotStacks, jobLevel >= 80 ? 2 : 1);

				case PowerType.FirstmindsFocus:
					if (jobLevel < 90)
					{
						return (0, 0);
					}

					return (Service.JobGauges.Get<DRGGauge>().FirstmindsFocusCount, 2);

				case PowerType.EyeOfTheDragon:
					if (jobLevel < 60)
					{
						return (0, 0);
					}

					return (Service.JobGauges.Get<DRGGauge>().EyeCount, 2);

				case PowerType.Aetherflow:
					if (jobLevel < 10)
					{
						return (0, 0);
					}

					return (SpellHelper.GetStatus(304u, Unit.Player)?.StackCount ?? 0, 2);

				default:
					return (0, 0);
			}
		}

		public static JobRoles RoleForJob(uint jobId)
		{
			if (JobRolesMap.TryGetValue(jobId, out JobRoles role))
			{
				return role;
			}

			return JobRoles.Unknown;
		}

		public static bool IsJobARole(uint jobId, JobRoles role)
		{
			if (JobRolesMap.TryGetValue(jobId, out JobRoles r))
			{
				return r == role;
			}

			return false;
		}

		public static bool IsJobTank(uint jobId) => IsJobARole(jobId, JobRoles.Tank);

		public static bool IsJobWithCleanse(uint jobId, int level)
		{
			bool isOnCleanseJob = _cleanseJobs.Contains(jobId);

			if (jobId == JobIDs.BRD && level < 35)
			{
				isOnCleanseJob = false;
			}

			return isOnCleanseJob;
		}

		private static readonly List<uint> _cleanseJobs = new()
		{
			JobIDs.CNJ,
			JobIDs.WHM,
			JobIDs.SCH,
			JobIDs.AST,
			JobIDs.SGE,
			JobIDs.BRD,
			JobIDs.BLU
		};

		public static bool IsJobHealer(uint jobId) => IsJobARole(jobId, JobRoles.Healer);

		public static bool IsJobDPS(uint jobId)
		{
			if (JobRolesMap.TryGetValue(jobId, out JobRoles r))
			{
				return r == JobRoles.DPSMelee || r == JobRoles.DPSRanged || r == JobRoles.DPSCaster;
			}

			return false;
		}

		public static bool IsJobDPSMelee(uint jobId) => IsJobARole(jobId, JobRoles.DPSMelee);

		public static bool IsJobDPSRanged(uint jobId) => IsJobARole(jobId, JobRoles.DPSRanged);

		public static bool IsJobDPSCaster(uint jobId) => IsJobARole(jobId, JobRoles.DPSCaster);

		public static bool IsJobCrafter(uint jobId) => IsJobARole(jobId, JobRoles.Crafter);

		public static bool IsJobGatherer(uint jobId) => IsJobARole(jobId, JobRoles.Gatherer);

		public static bool IsJobWithRaise(uint jobId, uint level)
		{
			bool isOnRaiseJob = _raiseJobs.Contains(jobId);

			if (jobId == JobIDs.RDM && level < 64 || level < 12)
			{
				isOnRaiseJob = false;
			}

			return isOnRaiseJob;
		}

		private static readonly List<uint> _raiseJobs = new()
		{
			JobIDs.CNJ,
			JobIDs.WHM,
			JobIDs.SCH,
			JobIDs.AST,
			JobIDs.RDM,
			JobIDs.SMN,
			JobIDs.SGE
		};

		public static uint IconIDForJob(uint jobId) => jobId + 62000;

		public static uint RoleIconIDForJob(uint jobId, bool specificDPSIcons = false)
		{
			JobRoles role = RoleForJob(jobId);

			switch (role)
			{
				case JobRoles.Tank: return 62581;
				case JobRoles.Healer: return 62582;

				case JobRoles.DPSMelee:
				case JobRoles.DPSRanged:
				case JobRoles.DPSCaster:
					if (specificDPSIcons && SpecificDPSIcons.TryGetValue(jobId, out uint iconId))
					{
						return iconId;
					}
					else
					{
						return 62583;
					}

				case JobRoles.Gatherer:
				case JobRoles.Crafter:
					return IconIDForJob(jobId);
			}

			return 0;
		}

		public static uint GetParentJobId(uint jobId)
		{
			ExcelSheet<ClassJob>? classJobSheet = Service.DataManager.GetExcelSheet<ClassJob>();
			return classJobSheet?.GetRow(jobId)?.ClassJobParent.Row ?? 0;
		}

		public static uint RoleIconIDForBattleCompanion => 62041;

		public static Dictionary<uint, JobRoles> JobRolesMap = new()
		{
			// Tanks
			[JobIDs.GLA] = JobRoles.Tank,
			[JobIDs.MRD] = JobRoles.Tank,
			[JobIDs.PLD] = JobRoles.Tank,
			[JobIDs.WAR] = JobRoles.Tank,
			[JobIDs.DRK] = JobRoles.Tank,
			[JobIDs.GNB] = JobRoles.Tank,

			// Healers
			[JobIDs.CNJ] = JobRoles.Healer,
			[JobIDs.WHM] = JobRoles.Healer,
			[JobIDs.SCH] = JobRoles.Healer,
			[JobIDs.AST] = JobRoles.Healer,
			[JobIDs.SGE] = JobRoles.Healer,

			// Melees
			[JobIDs.PGL] = JobRoles.DPSMelee,
			[JobIDs.LNC] = JobRoles.DPSMelee,
			[JobIDs.ROG] = JobRoles.DPSMelee,
			[JobIDs.MNK] = JobRoles.DPSMelee,
			[JobIDs.DRG] = JobRoles.DPSMelee,
			[JobIDs.NIN] = JobRoles.DPSMelee,
			[JobIDs.SAM] = JobRoles.DPSMelee,
			[JobIDs.RPR] = JobRoles.DPSMelee,

			// Ranged
			[JobIDs.ARC] = JobRoles.DPSRanged,
			[JobIDs.BRD] = JobRoles.DPSRanged,
			[JobIDs.MCH] = JobRoles.DPSRanged,
			[JobIDs.DNC] = JobRoles.DPSRanged,

			// Casters
			[JobIDs.THM] = JobRoles.DPSCaster,
			[JobIDs.ACN] = JobRoles.DPSCaster,
			[JobIDs.BLM] = JobRoles.DPSCaster,
			[JobIDs.SMN] = JobRoles.DPSCaster,
			[JobIDs.RDM] = JobRoles.DPSCaster,
			[JobIDs.BLU] = JobRoles.DPSCaster,

			// Crafters
			[JobIDs.CRP] = JobRoles.Crafter,
			[JobIDs.BSM] = JobRoles.Crafter,
			[JobIDs.ARM] = JobRoles.Crafter,
			[JobIDs.GSM] = JobRoles.Crafter,
			[JobIDs.LTW] = JobRoles.Crafter,
			[JobIDs.WVR] = JobRoles.Crafter,
			[JobIDs.ALC] = JobRoles.Crafter,
			[JobIDs.CUL] = JobRoles.Crafter,

			// Gatherers
			[JobIDs.MIN] = JobRoles.Gatherer,
			[JobIDs.BOT] = JobRoles.Gatherer,
			[JobIDs.FSH] = JobRoles.Gatherer
		};

		public static Dictionary<JobRoles, List<uint>> JobsByRole = new()
		{
			// Tanks
			[JobRoles.Tank] = new()
			{
				JobIDs.GLA,
				JobIDs.MRD,
				JobIDs.PLD,
				JobIDs.WAR,
				JobIDs.DRK,
				JobIDs.GNB
			},

			// Healers
			[JobRoles.Healer] = new()
			{
				JobIDs.CNJ,
				JobIDs.WHM,
				JobIDs.SCH,
				JobIDs.AST,
				JobIDs.SGE
			},

			// Melees
			[JobRoles.DPSMelee] = new()
			{
				JobIDs.PGL,
				JobIDs.LNC,
				JobIDs.ROG,
				JobIDs.MNK,
				JobIDs.DRG,
				JobIDs.NIN,
				JobIDs.SAM,
				JobIDs.RPR
			},

			// Ranged
			[JobRoles.DPSRanged] = new()
			{
				JobIDs.ARC,
				JobIDs.BRD,
				JobIDs.MCH,
				JobIDs.DNC
			},

			// Casters
			[JobRoles.DPSCaster] = new()
			{
				JobIDs.THM,
				JobIDs.ACN,
				JobIDs.BLM,
				JobIDs.SMN,
				JobIDs.RDM,
				JobIDs.BLU
			},

			// Crafters
			[JobRoles.Crafter] = new()
			{
				JobIDs.CRP,
				JobIDs.BSM,
				JobIDs.ARM,
				JobIDs.GSM,
				JobIDs.LTW,
				JobIDs.WVR,
				JobIDs.ALC,
				JobIDs.CUL
			},

			// Gatherers
			[JobRoles.Gatherer] = new()
			{
				JobIDs.MIN,
				JobIDs.BOT,
				JobIDs.FSH
			},

			// Unknown
			[JobRoles.Unknown] = new()
		};

		public static Dictionary<uint, string> JobNames = new()
		{
			// Tanks
			[JobIDs.GLA] = "GLA",
			[JobIDs.MRD] = "MRD",
			[JobIDs.PLD] = "PLD",
			[JobIDs.WAR] = "WAR",
			[JobIDs.DRK] = "DRK",
			[JobIDs.GNB] = "GNB",

			// Melees
			[JobIDs.PGL] = "PGL",
			[JobIDs.LNC] = "LNC",
			[JobIDs.ROG] = "ROG",
			[JobIDs.MNK] = "MNK",
			[JobIDs.DRG] = "DRG",
			[JobIDs.NIN] = "NIN",
			[JobIDs.SAM] = "SAM",
			[JobIDs.RPR] = "RPR",

			// Ranged
			[JobIDs.ARC] = "ARC",
			[JobIDs.BRD] = "BRD",
			[JobIDs.MCH] = "MCH",
			[JobIDs.DNC] = "DNC",

			// Casters
			[JobIDs.THM] = "THM",
			[JobIDs.ACN] = "ACN",
			[JobIDs.BLM] = "BLM",
			[JobIDs.SMN] = "SMN",
			[JobIDs.RDM] = "RDM",
			[JobIDs.BLU] = "BLU",

			// Healers
			[JobIDs.CNJ] = "CNJ",
			[JobIDs.WHM] = "WHM",
			[JobIDs.SCH] = "SCH",
			[JobIDs.SGE] = "SGE",
			[JobIDs.AST] = "AST",

			// Crafters
			[JobIDs.CRP] = "CRP",
			[JobIDs.BSM] = "BSM",
			[JobIDs.ARM] = "ARM",
			[JobIDs.GSM] = "GSM",
			[JobIDs.LTW] = "LTW",
			[JobIDs.WVR] = "WVR",
			[JobIDs.ALC] = "ALC",
			[JobIDs.CUL] = "CUL",

			// Gatherers
			[JobIDs.MIN] = "MIN",
			[JobIDs.BOT] = "BOT",
			[JobIDs.FSH] = "FSH"
		};

		public static Dictionary<JobRoles, string> RoleNames = new()
		{
			[JobRoles.Tank] = "Tank",
			[JobRoles.Healer] = "Healer",
			[JobRoles.DPSMelee] = "Melee",
			[JobRoles.DPSRanged] = "Ranged",
			[JobRoles.DPSCaster] = "Caster",
			[JobRoles.Crafter] = "Crafter",
			[JobRoles.Gatherer] = "Gatherer",
			[JobRoles.Unknown] = "Unknown"
		};

		public static Dictionary<uint, uint> SpecificDPSIcons = new()
		{
			// Melees
			[JobIDs.PGL] = 62584,
			[JobIDs.LNC] = 62584,
			[JobIDs.ROG] = 62584,
			[JobIDs.MNK] = 62584,
			[JobIDs.DRG] = 62584,
			[JobIDs.NIN] = 62584,
			[JobIDs.SAM] = 62584,
			[JobIDs.RPR] = 62584,

			// Ranged
			[JobIDs.ARC] = 62586,
			[JobIDs.BRD] = 62586,
			[JobIDs.MCH] = 62586,
			[JobIDs.DNC] = 62586,

			// Casters
			[JobIDs.THM] = 62587,
			[JobIDs.ACN] = 62587,
			[JobIDs.BLM] = 62587,
			[JobIDs.SMN] = 62587,
			[JobIDs.RDM] = 62587,
			[JobIDs.BLU] = 62587
		};
	}

	public static class JobIDs
	{
		public const uint GLA = 1;
		public const uint MRD = 3;
		public const uint PLD = 19;
		public const uint WAR = 21;
		public const uint DRK = 32;
		public const uint GNB = 37;

		public const uint CNJ = 6;
		public const uint WHM = 24;
		public const uint SCH = 28;
		public const uint AST = 33;
		public const uint SGE = 40;

		public const uint PGL = 2;
		public const uint LNC = 4;
		public const uint ROG = 29;
		public const uint MNK = 20;
		public const uint DRG = 22;
		public const uint NIN = 30;
		public const uint SAM = 34;
		public const uint RPR = 39;

		public const uint ARC = 5;
		public const uint BRD = 23;
		public const uint MCH = 31;
		public const uint DNC = 38;

		public const uint THM = 7;
		public const uint ACN = 26;
		public const uint BLM = 25;
		public const uint SMN = 27;
		public const uint RDM = 35;
		public const uint BLU = 36;

		public const uint CRP = 8;
		public const uint BSM = 9;
		public const uint ARM = 10;
		public const uint GSM = 11;
		public const uint LTW = 12;
		public const uint WVR = 13;
		public const uint ALC = 14;
		public const uint CUL = 15;

		public const uint MIN = 16;
		public const uint BOT = 17;
		public const uint FSH = 18;
	}
}