﻿using System;
using System.Collections.Generic;
using DelvUI.Helpers;
using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Enums;

namespace SezzUI.Interface.GeneralElements
{
	public class GlobalColors : IDisposable
	{
		#region Singleton

		private MiscColorConfig _miscColorConfig = null!;
		private RolesColorConfig _rolesColorConfig = null!;

		private Dictionary<uint, PluginConfigColor> ColorMap = null!;

		private GlobalColors()
		{
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
			OnConfigReset(ConfigurationManager.Instance);
		}

		private void OnConfigReset(ConfigurationManager sender)
		{
			_miscColorConfig = sender.GetConfigObject<MiscColorConfig>();
			_rolesColorConfig = sender.GetConfigObject<RolesColorConfig>();

			TanksColorConfig tanksColorConfig = sender.GetConfigObject<TanksColorConfig>();
			HealersColorConfig healersColorConfig = sender.GetConfigObject<HealersColorConfig>();
			MeleeColorConfig meleeColorConfig = sender.GetConfigObject<MeleeColorConfig>();
			RangedColorConfig rangedColorConfig = sender.GetConfigObject<RangedColorConfig>();
			CastersColorConfig castersColorConfig = sender.GetConfigObject<CastersColorConfig>();

			ColorMap = new()
			{
				// Tanks
				[JobIDs.GLA] = tanksColorConfig.GLAColor,
				[JobIDs.MRD] = tanksColorConfig.MRDColor,
				[JobIDs.PLD] = tanksColorConfig.PLDColor,
				[JobIDs.WAR] = tanksColorConfig.WARColor,
				[JobIDs.DRK] = tanksColorConfig.DRKColor,
				[JobIDs.GNB] = tanksColorConfig.GNBColor,

				// Healers
				[JobIDs.CNJ] = healersColorConfig.CNJColor,
				[JobIDs.WHM] = healersColorConfig.WHMColor,
				[JobIDs.SCH] = healersColorConfig.SCHColor,
				[JobIDs.AST] = healersColorConfig.ASTColor,
				[JobIDs.SGE] = healersColorConfig.SGEColor,

				// Melee
				[JobIDs.PGL] = meleeColorConfig.PGLColor,
				[JobIDs.LNC] = meleeColorConfig.LNCColor,
				[JobIDs.ROG] = meleeColorConfig.ROGColor,
				[JobIDs.MNK] = meleeColorConfig.MNKColor,
				[JobIDs.DRG] = meleeColorConfig.DRGColor,
				[JobIDs.NIN] = meleeColorConfig.NINColor,
				[JobIDs.SAM] = meleeColorConfig.SAMColor,
				[JobIDs.RPR] = meleeColorConfig.RPRColor,

				// Ranged 
				[JobIDs.ARC] = rangedColorConfig.ARCColor,
				[JobIDs.BRD] = rangedColorConfig.BRDColor,
				[JobIDs.MCH] = rangedColorConfig.MCHColor,
				[JobIDs.DNC] = rangedColorConfig.DNCColor,

				// Casters
				[JobIDs.THM] = castersColorConfig.THMColor,
				[JobIDs.ACN] = castersColorConfig.ACNColor,
				[JobIDs.BLM] = castersColorConfig.BLMColor,
				[JobIDs.SMN] = castersColorConfig.SMNColor,
				[JobIDs.RDM] = castersColorConfig.RDMColor,
				[JobIDs.BLU] = castersColorConfig.BLUColor,

				// Crafters
				[JobIDs.CRP] = _rolesColorConfig.HANDColor,
				[JobIDs.BSM] = _rolesColorConfig.HANDColor,
				[JobIDs.ARM] = _rolesColorConfig.HANDColor,
				[JobIDs.GSM] = _rolesColorConfig.HANDColor,
				[JobIDs.LTW] = _rolesColorConfig.HANDColor,
				[JobIDs.WVR] = _rolesColorConfig.HANDColor,
				[JobIDs.ALC] = _rolesColorConfig.HANDColor,
				[JobIDs.CUL] = _rolesColorConfig.HANDColor,

				// Gatherers
				[JobIDs.MIN] = _rolesColorConfig.LANDColor,
				[JobIDs.BOT] = _rolesColorConfig.LANDColor,
				[JobIDs.FSH] = _rolesColorConfig.LANDColor
			};
		}

		public static void Initialize()
		{
			Instance = new();
		}

		public static GlobalColors Instance { get; private set; } = null!;

		~GlobalColors()
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

			ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
			Instance = null!;
		}

		#endregion

		public PluginConfigColor? ColorForJobId(uint jobId) => ColorMap.TryGetValue(jobId, out PluginConfigColor? color) ? color : null;

		public PluginConfigColor SafeColorForJobId(uint jobId) => ColorForJobId(jobId) ?? _miscColorConfig.NPCNeutralColor;

		public PluginConfigColor? RoleColorForJobId(uint jobId)
		{
			JobRoles role = JobsHelper.RoleForJob(jobId);

			return role switch
			{
				JobRoles.Tank => _rolesColorConfig.TankRoleColor,
				JobRoles.Healer => _rolesColorConfig.HealerRoleColor,
				JobRoles.DPSMelee => _rolesColorConfig.UseSpecificDPSColors ? _rolesColorConfig.MeleeDPSRoleColor : _rolesColorConfig.DPSRoleColor,
				JobRoles.DPSRanged => _rolesColorConfig.UseSpecificDPSColors ? _rolesColorConfig.RangedDPSRoleColor : _rolesColorConfig.DPSRoleColor,
				JobRoles.DPSCaster => _rolesColorConfig.UseSpecificDPSColors ? _rolesColorConfig.CasterDPSRoleColor : _rolesColorConfig.DPSRoleColor,
				JobRoles.Gatherer => _rolesColorConfig.LANDColor,
				JobRoles.Crafter => _rolesColorConfig.HANDColor,
				_ => null
			};
		}

		public PluginConfigColor SafeRoleColorForJobId(uint jobId) => RoleColorForJobId(jobId) ?? _miscColorConfig.NPCNeutralColor;

		public PluginConfigColor EmptyColor => _miscColorConfig.EmptyColor;
		public PluginConfigColor PartialFillColor => _miscColorConfig.PartialFillColor;
		public PluginConfigColor NPCFriendlyColor => _miscColorConfig.NPCFriendlyColor;
		public PluginConfigColor NPCHostileColor => _miscColorConfig.NPCHostileColor;
		public PluginConfigColor NPCNeutralColor => _miscColorConfig.NPCNeutralColor;
	}

	[Disableable(false)]
	[Section("Colors")]
	[SubSection("Tanks", 0)]
	public class TanksColorConfig : PluginConfigObject
	{
		public new static TanksColorConfig DefaultConfig() => new();

		[ColorEdit4("Paladin", spacing = true)]
		[Order(5)]
		public PluginConfigColor PLDColor = new(new(168f / 255f, 210f / 255f, 230f / 255f, 100f / 100f));

		[ColorEdit4("Dark Knight")]
		[Order(10)]
		public PluginConfigColor DRKColor = new(new(209f / 255f, 38f / 255f, 204f / 255f, 100f / 100f));

		[ColorEdit4("Warrior")]
		[Order(15)]
		public PluginConfigColor WARColor = new(new(207f / 255f, 38f / 255f, 33f / 255f, 100f / 100f));

		[ColorEdit4("Gunbreaker")]
		[Order(20)]
		public PluginConfigColor GNBColor = new(new(121f / 255f, 109f / 255f, 48f / 255f, 100f / 100f));

		[ColorEdit4("Gladiator", spacing = true)]
		[Order(25)]
		public PluginConfigColor GLAColor = new(new(168f / 255f, 210f / 255f, 230f / 255f, 100f / 100f));

		[ColorEdit4("Marauder")]
		[Order(30)]
		public PluginConfigColor MRDColor = new(new(207f / 255f, 38f / 255f, 33f / 255f, 100f / 100f));
	}

	[Disableable(false)]
	[Section("Colors")]
	[SubSection("Healers", 0)]
	public class HealersColorConfig : PluginConfigObject
	{
		public new static HealersColorConfig DefaultConfig() => new();

		[ColorEdit4("Scholar", spacing = true)]
		[Order(5)]
		public PluginConfigColor SCHColor = new(new(134f / 255f, 87f / 255f, 255f / 255f, 100f / 100f));

		[ColorEdit4("White Mage")]
		[Order(10)]
		public PluginConfigColor WHMColor = new(new(255f / 255f, 240f / 255f, 220f / 255f, 100f / 100f));

		[ColorEdit4("Astrologian")]
		[Order(15)]
		public PluginConfigColor ASTColor = new(new(255f / 255f, 231f / 255f, 74f / 255f, 100f / 100f));

		[ColorEdit4("Sage")]
		[Order(20)]
		public PluginConfigColor SGEColor = new(new(144f / 255f, 176f / 255f, 255f / 255f, 100f / 100f));

		[ColorEdit4("Conjurer", spacing = true)]
		[Order(25)]
		public PluginConfigColor CNJColor = new(new(255f / 255f, 240f / 255f, 220f / 255f, 100f / 100f));
	}

	[Disableable(false)]
	[Section("Colors")]
	[SubSection("Melee", 0)]
	public class MeleeColorConfig : PluginConfigObject
	{
		public new static MeleeColorConfig DefaultConfig() => new();

		[ColorEdit4("Monk", spacing = true)]
		[Order(5)]
		public PluginConfigColor MNKColor = new(new(214f / 255f, 156f / 255f, 0f / 255f, 100f / 100f));

		[ColorEdit4("Ninja")]
		[Order(10)]
		public PluginConfigColor NINColor = new(new(175f / 255f, 25f / 255f, 100f / 255f, 100f / 100f));

		[ColorEdit4("Dragoon")]
		[Order(15)]
		public PluginConfigColor DRGColor = new(new(65f / 255f, 100f / 255f, 205f / 255f, 100f / 100f));

		[ColorEdit4("Samurai")]
		[Order(20)]
		public PluginConfigColor SAMColor = new(new(228f / 255f, 109f / 255f, 4f / 255f, 100f / 100f));

		[ColorEdit4("Reaper")]
		[Order(25)]
		public PluginConfigColor RPRColor = new(new(150f / 255f, 90f / 255f, 144f / 255f, 100f / 100f));

		[ColorEdit4("Pugilist", spacing = true)]
		[Order(30)]
		public PluginConfigColor PGLColor = new(new(214f / 255f, 156f / 255f, 0f / 255f, 100f / 100f));

		[ColorEdit4("Rogue")]
		[Order(35)]
		public PluginConfigColor ROGColor = new(new(175f / 255f, 25f / 255f, 100f / 255f, 100f / 100f));

		[ColorEdit4("Lancer")]
		[Order(40)]
		public PluginConfigColor LNCColor = new(new(65f / 255f, 100f / 255f, 205f / 255f, 100f / 100f));
	}

	[Disableable(false)]
	[Section("Colors")]
	[SubSection("Ranged", 0)]
	public class RangedColorConfig : PluginConfigObject
	{
		public new static RangedColorConfig DefaultConfig() => new();

		[ColorEdit4("Bard", spacing = true)]
		[Order(5)]
		public PluginConfigColor BRDColor = new(new(145f / 255f, 186f / 255f, 94f / 255f, 100f / 100f));

		[ColorEdit4("Machinist")]
		[Order(10)]
		public PluginConfigColor MCHColor = new(new(110f / 255f, 225f / 255f, 214f / 255f, 100f / 100f));

		[ColorEdit4("Dancer")]
		[Order(15)]
		public PluginConfigColor DNCColor = new(new(226f / 255f, 176f / 255f, 175f / 255f, 100f / 100f));

		[ColorEdit4("Archer", separator = true)]
		[Order(20)]
		public PluginConfigColor ARCColor = new(new(145f / 255f, 186f / 255f, 94f / 255f, 100f / 100f));
	}

	[Disableable(false)]
	[Section("Colors")]
	[SubSection("Caster", 0)]
	public class CastersColorConfig : PluginConfigObject
	{
		public new static CastersColorConfig DefaultConfig() => new();

		[ColorEdit4("Black Mage", spacing = true)]
		[Order(5)]
		public PluginConfigColor BLMColor = new(new(165f / 255f, 121f / 255f, 214f / 255f, 100f / 100f));

		[ColorEdit4("Summoner")]
		[Order(10)]
		public PluginConfigColor SMNColor = new(new(45f / 255f, 155f / 255f, 120f / 255f, 100f / 100f));

		[ColorEdit4("Red Mage")]
		[Order(15)]
		public PluginConfigColor RDMColor = new(new(232f / 255f, 123f / 255f, 123f / 255f, 100f / 100f));

		[ColorEdit4("Blue Mage", spacing = true)]
		[Order(20)]
		public PluginConfigColor BLUColor = new(new(0f / 255f, 185f / 255f, 247f / 255f, 100f / 100f));

		[ColorEdit4("Thaumaturge")]
		[Order(25)]
		public PluginConfigColor THMColor = new(new(165f / 255f, 121f / 255f, 214f / 255f, 100f / 100f));

		[ColorEdit4("Arcanist")]
		[Order(30)]
		public PluginConfigColor ACNColor = new(new(45f / 255f, 155f / 255f, 120f / 255f, 100f / 100f));
	}

	[Disableable(false)]
	[Section("Colors")]
	[SubSection("Roles", 0)]
	public class RolesColorConfig : PluginConfigObject
	{
		public new static RolesColorConfig DefaultConfig() => new();

		[ColorEdit4("Tank")]
		[Order(10)]
		public PluginConfigColor TankRoleColor = new(new(21f / 255f, 28f / 255f, 100f / 255f, 100f / 100f));

		[ColorEdit4("DPS")]
		[Order(15)]
		public PluginConfigColor DPSRoleColor = new(new(153f / 255f, 23f / 255f, 23f / 255f, 100f / 100f));

		[ColorEdit4("Healer")]
		[Order(20)]
		public PluginConfigColor HealerRoleColor = new(new(46f / 255f, 125f / 255f, 50f / 255f, 100f / 100f));

		[ColorEdit4("Disciple of the Land", spacing = true)]
		[Order(25)]
		public PluginConfigColor LANDColor = new(new(99f / 255f, 172f / 255f, 14f / 255f, 100f / 100f));

		[ColorEdit4("Disciple of the Hand")]
		[Order(30)]
		public PluginConfigColor HANDColor = new(new(99f / 255f, 172f / 255f, 14f / 255f, 100f / 100f));

		[Checkbox("Use Specific DPS Colors", spacing = true)]
		[Order(35)]
		public bool UseSpecificDPSColors = false;

		[ColorEdit4("Melee DPS")]
		[Order(40, collapseWith = nameof(UseSpecificDPSColors))]
		public PluginConfigColor MeleeDPSRoleColor = new(new(151f / 255f, 56f / 255f, 56f / 255f, 100f / 100f));

		[ColorEdit4("Ranged DPS")]
		[Order(40, collapseWith = nameof(UseSpecificDPSColors))]
		public PluginConfigColor RangedDPSRoleColor = new(new(250f / 255f, 185f / 255f, 67f / 255f, 100f / 100f));

		[ColorEdit4("Caster DPS")]
		[Order(40, collapseWith = nameof(UseSpecificDPSColors))]
		public PluginConfigColor CasterDPSRoleColor = new(new(154f / 255f, 82f / 255f, 193f / 255f, 100f / 100f));
	}

	[Disableable(false)]
	[Section("Colors")]
	[SubSection("Misc", 0)]
	public class MiscColorConfig : PluginConfigObject
	{
		public new static MiscColorConfig DefaultConfig() => new();

		[ColorEdit4("Empty Bar")]
		[Order(10)]
		public PluginConfigColor EmptyColor = new(new(0f / 255f, 0f / 255f, 0f / 255f, 50f / 100f));

		[ColorEdit4("Partially Filled Bar")]
		[Order(15)]
		public PluginConfigColor PartialFillColor = new(new(180f / 255f, 180f / 255f, 180f / 255f, 100f / 100f));

		[ColorEdit4("NPC Friendly", separator = true)]
		[Order(20)]
		public PluginConfigColor NPCFriendlyColor = new(new(99f / 255f, 172f / 255f, 14f / 255f, 100f / 100f));

		[ColorEdit4("NPC Hostile")]
		[Order(25)]
		public PluginConfigColor NPCHostileColor = new(new(233f / 255f, 4f / 255f, 4f / 255f, 100f / 100f));

		[ColorEdit4("NPC Neutral")]
		[Order(30)]
		public PluginConfigColor NPCNeutralColor = new(new(218f / 255f, 157f / 255f, 46f / 255f, 100f / 100f));
	}

	[Exportable(false)]
	public class ColorByHealthValueConfig : PluginConfigObject
	{
		[Checkbox("Use Max Health Color")]
		[Order(5)]
		public bool UseMaxHealthColor = false;

		[ColorEdit4("Max Health Color")]
		[Order(10, collapseWith = nameof(UseMaxHealthColor))]
		public PluginConfigColor MaxHealthColor = new(new(18f / 255f, 18f / 255f, 18f / 255f, 100f / 100f));

		[ColorEdit4("High Health Color")]
		[Order(15)]
		public PluginConfigColor FullHealthColor = new(new(0f / 255f, 255f / 255f, 0f / 255f, 100f / 100f));

		[ColorEdit4("Low Health Color")]
		[Order(20)]
		public PluginConfigColor LowHealthColor = new(new(255f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));

		[DragFloat("Max Health Color Above Health %", min = 50f, max = 100f, velocity = 1f)]
		[Order(25)]
		public float FullHealthColorThreshold = 75f;

		[DragFloat("Low Health Color Below Health %", min = 0f, max = 50f, velocity = 1f)]
		[Order(30)]
		public float LowHealthColorThreshold = 25f;

		[Combo("Blend Mode", "LAB", "LChab", "XYZ", "RGB", "LChuv", "Luv", "Jzazbz", "JzCzhz")]
		[Order(35)]
		public BlendMode BlendMode = BlendMode.LAB;
	}
}