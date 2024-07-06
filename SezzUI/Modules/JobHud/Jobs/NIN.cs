using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;
using SezzUI.Enums;
using SezzUI.Game;
using SezzUI.Game.Events;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class NIN : BasePreset
{
	public override uint JobId => JobIDs.NIN;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 2269, CustomDuration = GetHutonDuration, StatusWarningThreshold = 20}); // Huton
		bar1.Add(new(bar1) {TextureActionId = 2258, CooldownActionId = 2258, StatusId = 638, StatusTarget = Unit.Target, MaxStatusDuration = 15, StatusSourcePlayer = false, GlowBorderUsable = true, CustomCondition = IsHidden}); // Trick Attack
		bar1.Add(new(bar1) {TextureActionId = 2264, CooldownActionId = 2264, StatusId = 497, MaxStatusDuration = 15}); // Kassatsu
		bar1.Add(new(bar1) {TextureActionId = 16493, CooldownActionId = 16493, StatusId = 1954, MaxStatusDuration = 30, RequiredPowerType = JobsHelper.PowerType.Ninki, RequiredPowerAmount = 50}); // Bunshin
		bar1.Add(new(bar1) {TextureActionId = 7403, CooldownActionId = 7403, StatusId = 1186, MaxStatusDuration = 6}); // Ten Chi Jin
		hud.AddBar(bar1);

		Bar bar2 = new(hud);
		bar2.Add(new(bar2) {TextureActionId = 2245, CooldownActionId = 2245, StatusIds = new[] {507u, 614u}, MaxStatusDurations = new[] {20f, Constants.PERMANENT_STATUS_DURATION}, CustomCondition = IsOutOfCombat}); // Hide
		bar2.Add(new(bar2) {TextureActionId = 16489, CooldownActionId = 16489, CustomPowerCondition = IsMeisuiUsable, RequiresCombat = true}); // Meisui
		bar2.Add(new(bar2) {TextureActionId = 7402, RequiredPowerType = JobsHelper.PowerType.Ninki, RequiredPowerAmount = 50, GlowBorderUsable = true, StacksPowerType = JobsHelper.PowerType.Ninki}); // Bhavacakra
		bar2.Add(new(bar2) {TextureActionId = 2262, CooldownActionId = 2262}); // Shukuchi
		hud.AddBar(bar2);

		// Ten Chi Jin
		using (AuraAlert aa = new())
		{
			aa.StatusId = 1186u;
			aa.Size = new(48, 48);
			aa.Position = Vector2.Zero;
			aa.Level = 70;
			aa.BorderSize = 1;
			aa.UseActionIcon(7403);
			aa.GlowBackdrop = true;
			aa.GlowColor = new(188f / 255f, 35f / 255f, 35f / 255f, 0.5f);
			aa.GlowBackdropSize = 4;
			aa.MaxDuration = 6;
			hud.AddAlert(aa);
		}

		base.Configure(hud);

		Bar roleBar = hud.Bars.Last();
		roleBar.Add(new(roleBar) {TextureActionId = 2241, CooldownActionId = 2241, StatusId = 488, MaxStatusDuration = 20}, 1); // Shade Shift
	}

	private static bool IsMeisuiUsable() => Services.JobGauges.Get<NINGauge>().Ninki <= 50 && IsHidden();

	private static bool IsHidden() => SpellHelper.GetStatus(507, Unit.Player) != null || SpellHelper.GetStatus(614, Unit.Player) != null;

	private static (float, float) GetHutonDuration()
	{
		NINGauge gauge = Services.JobGauges.Get<NINGauge>();
		// TODO
		// if (gauge.HutonTimer != 0)
		// {
		// 	return (gauge.HutonTimer / 1000f, 60f);
		// }

		return (0, 0);
	}

	private static bool IsOutOfCombat() => !EventManager.Combat.IsInCombat(false);
}