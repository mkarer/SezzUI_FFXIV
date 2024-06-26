﻿using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Statuses;
using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class RPR : BasePreset
{
	public override uint JobId => JobIDs.RPR;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureStatusId = 2586, StatusId = 2586, MaxStatusDuration = 60, StatusTarget = Unit.Target}); // Death's Design
		bar1.Add(new(bar1) {TextureActionId = 24394, StatusIds = new[] {2593u, 2863u}, MaxStatusDuration = 30, RequiredPowerAmount = 50, RequiredPowerType = JobsHelper.PowerType.Shroud, GlowBorderUsable = true}); // Enshroud
		bar1.Add(new(bar1) {TextureActionId = 24398, CustomCondition = IsEnshrouded, CustomPowerCondition = HasOneLemureLeft, GlowBorderUsable = true}); // Communio
		bar1.Add(new(bar1) {TextureActionId = 24393, CooldownActionId = 24393, RequiredPowerAmount = 50, RequiredPowerType = JobsHelper.PowerType.Soul, GlowBorderUsable = true}); // Gluttony
		bar1.Add(new(bar1) {TextureActionId = 24405, CooldownActionId = 24405, StatusId = 2599, MaxStatusDuration = 20, StacksStatusId = 2592}); // Arcane Circle
		hud.AddBar(bar1);

		// Soulsow/Harvest Moon
		hud.AddAlert(new()
		{
			StatusId = 2594,
			InvertCheck = true,
			EnableInCombat = false,
			EnableOutOfCombat = true,
			TreatWeaponOutAsCombat = false,
			Size = new(128, 256),
			Image = "surge_of_darkness.png",
			Position = new(-140, 50),
			Level = 82
		});

		// Immortal Sacrifice (Plentiful Harvest)
		hud.AddAlert(new()
		{
			StatusId = 2592,
			MaxDuration = 30,
			CustomCondition = HasNoBloodsownCircle,
			Size = new Vector2(256, 128) * 1.1f,
			Image = "predatory_swiftness.png",
			Position = new(0, -180),
			Level = 88
		});

		// Enhanced Gallows
		hud.AddAlert(new()
		{
			CustomCondition = ShouldUseGallows,
			Size = new Vector2(256, 128) * 0.5f,
			Image = "genericarc_05_90.png",
			Position = new(0, -80),
			Color = new(0 / 255f, 221f / 255f, 210f / 255f, 1f),
			Level = 70
		});

		// Enhanced Gibbet
		hud.AddAlert(new()
		{
			CustomCondition = ShouldUseGibbet,
			Size = new Vector2(128, 256) * 0.5f,
			Image = "genericarc_05.png",
			Position = new(-120, 20),
			Color = new(0 / 255f, 221f / 255f, 210f / 255f, 1f),
			Level = 70
		});

		// Enhanced Gibbet
		hud.AddAlert(new()
		{
			CustomCondition = ShouldUseGibbet,
			Size = new Vector2(128, 256) * 0.5f,
			Image = "genericarc_05.png",
			Position = new(120, 20),
			Color = new(0 / 255f, 221f / 255f, 210f / 255f, 1f),
			Level = 70,
			FlipImageHorizontally = true
		});

		base.Configure(hud);

		Bar roleBar = hud.Bars.Last();
		roleBar.Add(new(roleBar) {TextureActionId = 24404, CooldownActionId = 24404, StatusIds = new[] {2596u, 2597u}, MaxStatusDuration = 5}, 1); // Arcane Crest
	}

	private static bool ShouldUseGibbet()
	{
		Status? statusSoulReaver = SpellHelper.GetStatus(2587, Unit.Player);
		if (statusSoulReaver == null)
		{
			return false;
		}

		Status? statusEnhancedGallows = SpellHelper.GetStatus(2589, Unit.Player);
		Status? statusEnhancedGibbet = SpellHelper.GetStatus(2588, Unit.Player);

		return statusEnhancedGibbet != null || statusEnhancedGallows == null;
	}

	private static bool ShouldUseGallows()
	{
		Status? statusSoulReaver = SpellHelper.GetStatus(2587, Unit.Player);
		if (statusSoulReaver == null)
		{
			return false;
		}

		Status? statusEnhancedGallows = SpellHelper.GetStatus(2589, Unit.Player); // 2856
		Status? statusEnhancedGibbet = SpellHelper.GetStatus(2588, Unit.Player);

		return statusEnhancedGallows != null || statusEnhancedGibbet == null;
	}

	private static bool IsEnshrouded() => Services.JobGauges.Get<RPRGauge>().EnshroudedTimeRemaining > 0;

	private static bool HasOneLemureLeft()
	{
		RPRGauge gauge = Services.JobGauges.Get<RPRGauge>();
		return gauge.EnshroudedTimeRemaining > 0 && gauge.LemureShroud == 1 && gauge.VoidShroud == 0;
	}

	private static bool HasNoBloodsownCircle() => SpellHelper.GetStatus(2972, Unit.Player) == null;
}