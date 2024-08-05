using Dalamud.Game.ClientState.JobGauge.Types;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class DRK : BasePreset
{
	public override uint JobId => JobIDs.DRK;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 25754, CooldownActionId = 25754, StatusId = 2682, MaxStatusDuration = 10}); // Oblation
		bar1.Add(new(bar1) {TextureActionId = 3625, CooldownActionId = 3625, StatusIds = new[] {742u, 1972u}, MaxStatusDuration = 10, GlowBorderStatusId = 1972, Features = IconFeatures.GlowIgnoresState}); // Blood Weapon/Delirium
		bar1.Add(new(bar1) {TextureActionId = 36926, CooldownActionId = 36926}); // Shadowstride
		bar1.Add(new(bar1) {TextureActionId = 16472, CooldownActionId = 16472, RequiredPowerType = JobsHelper.PowerType.Blood, RequiredPowerAmount = 50, CustomDuration = GetLivingShadowDuration}); // Living Shadow
		bar1.Add(new(bar1) {TextureActionId = 7393, CooldownActionId = 7393, StatusId = 1178, MaxStatusDuration = 7, RequiredPowerType = JobsHelper.PowerType.MP, RequiredPowerAmount = 3000}); // The Blackest Night
		hud.AddBar(bar1);

		Bar bar2 = new(hud);
		bar2.Add(new(bar2) {TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20}); // Rampart
		bar2.Add(new(bar2) {TextureActionId = 3636, CooldownActionId = 3636, StatusActionId = 3636, MaxStatusDuration = 15}); // Shadow Wall
		bar2.Add(new(bar2) {TextureActionId = 16471, CooldownActionId = 16471, StatusActionId = 16471, MaxStatusDuration = 15}); // Dark Missionary
		bar2.Add(new(bar2) {TextureActionId = 3634, CooldownActionId = 3634, StatusActionId = 3634, MaxStatusDuration = 10}); // Dark Mind
		bar2.Add(new(bar2) {TextureActionId = 3638, CooldownActionId = 3638, StatusActionId = 3638, MaxStatusDuration = 10}); // Living Dead
		hud.AddBar(bar2);

		base.Configure(hud);
	}

	private static (float, float) GetLivingShadowDuration()
	{
		DRKGauge gauge = Services.JobGauges.Get<DRKGauge>();
		if (gauge.ShadowTimeRemaining != 0)
		{
			return (gauge.ShadowTimeRemaining / 1000f, 24f);
		}

		return (0, 0);
	}
}