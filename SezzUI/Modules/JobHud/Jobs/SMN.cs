using System;
using System.Linq;
using Dalamud.Game.ClientState.JobGauge.Types;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class SMN : BasePreset
{
	public override uint JobId => JobIDs.SMN;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 25800, CooldownActionId = 25800, CustomDuration = GetDemiBahamutDuration, RequiresCombat = true, RequiresPet = true}); // Aethercharge/Dreadwyrm Trance
		bar1.Add(new(bar1) {TextureActionId = 7429, CooldownActionId = 7429, CustomCondition = IsDemiBahamutSummoned}); // Enkindle Bahamut
		bar1.Add(new(bar1) {TextureActionId = 181, RequiredPowerAmount = 1, RequiredPowerType = JobsHelper.PowerType.Aetherflow, StacksPowerType = JobsHelper.PowerType.Aetherflow, GlowBorderUsable = true}); // Fester
		bar1.Add(new(bar1) {TextureActionId = 25801, CooldownActionId = 25801, StatusId = 2703, MaxStatusDuration = 30, RequiresCombat = true, CustomCondition = IsCarbuncleSummoned, StatusSourcePlayer = false}); // Searing Light
		hud.AddBar(bar1);

		// Further Rain
		hud.AddAlert(new()
		{
			StatusId = 2701,
			MaxDuration = 60,
			Size = new(128, 256),
			Image = "demonic_core_vertical.png",
			Position = new(200, 50),
			Level = 62,
			FlipImageHorizontally = true
		});

		// Summon Carbuncle
		using (AuraAlert aa = new())
		{
			aa.CustomCondition = IsMissingPet;
			aa.Size = new(64, 64);
			aa.Position = new(0, -140);
			aa.Level = 2;
			aa.BorderSize = 1;
			aa.UseActionIcon(25798);
			aa.GlowBackdrop = true;
			aa.GlowColor = new(74f / 255f, 137f / 255f, 214f / 255f, 0.5f);
			aa.GlowBackdropSize = 4;
			hud.AddAlert(aa);
		}

		base.Configure(hud);

		Bar roleBar = hud.Bars.Last();
		roleBar.Add(new(roleBar) {TextureActionId = 25799, CooldownActionId = 25799, StatusId = 2702, MaxStatusDuration = 30, CustomCondition = IsCarbuncleSummoned, StatusSourcePlayer = false}, 1); // Radiant Aegis
	}

	// Pet IDs:
	// https://github.com/xivapi/ffxiv-datamining/blob/50f42f2ff396c7857ac2636e09ffbb4265a25dae/csv/Pet.csv
	// Carbuncle: 23
	// Demi-Bahamut: 10
	// Ifrit-Egi: 27
	// Titan-Egi: 28
	// Garuda-Egi: 29

	private static long _petSeen;

	public static bool IsMissingPet()
	{
		if ((Services.ClientState.LocalPlayer?.CurrentHp ?? 0) == 0)
		{
			return false;
		}

		long now = Environment.TickCount64;
		if (Services.BuddyList.PetBuddy != null)
		{
			_petSeen = now;
			return false;
		}

		return now - _petSeen > 2500;
	}

	private static bool IsCarbuncleSummoned() => Services.BuddyList.PetBuddy != null && Services.BuddyList.PetBuddy.PetData.Value.RowId == 23;

	private static bool IsDemiBahamutSummoned() => Services.BuddyList.PetBuddy != null && Services.BuddyList.PetBuddy.PetData.Value.RowId == 10;

	private static (float, float) GetDemiBahamutDuration()
	{
		if (IsDemiBahamutSummoned())
		{
			return (Services.JobGauges.Get<SMNGauge>().SummonTimerRemaining / 1000f, 15);
		}

		return (0, 0);
	}
}