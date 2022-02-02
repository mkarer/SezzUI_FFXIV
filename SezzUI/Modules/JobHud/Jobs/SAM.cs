using System.Linq;
using SezzUI.Enums;
using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class SAM : BasePreset
	{
		public override uint JobId => JobIDs.SAM;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureStatusId = 1299, StatusId = 1299, MaxStatusDuration = 40}); // Fuka
			bar1.Add(new(bar1) {TextureStatusId = 1298, StatusId = 1298, MaxStatusDuration = 40}); // Fugetsu
			bar1.Add(new(bar1) {TextureActionId = 7489, StatusIds = new[] {(uint) 1228, (uint) 1319}, MaxStatusDuration = 60, StatusTarget = Unit.Target, CustomPowerCondition = CanUseHiganbana}); // Higanbana
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 16482, CooldownActionId = 16482, RequiredPowerType = JobsHelper.PowerType.Kenki, RequiredPowerAmountMax = 50}); // Ikishoten
			bar2.Add(new(bar2) {TextureActionId = 7497, CooldownActionId = 7497, StatusId = 1231, MaxStatusDuration = 15, RequiredPowerType = JobsHelper.PowerType.MeditationStacks, RequiredPowerAmountMax = 2, StacksPowerType = JobsHelper.PowerType.MeditationStacks}); // Meditate
			bar2.Add(new(bar2) {TextureActionId = 7494, StatusId = 1229, MaxStatusDuration = 10, RequiredPowerType = JobsHelper.PowerType.Kenki, RequiredPowerAmount = 20}); // Hissatu: Kaiten
			bar2.Add(new(bar2) {TextureActionId = 7499, CooldownActionId = 7499, StatusId = 1233, MaxStatusDuration = 15}); // Meikyo Shisui
			hud.AddBar(bar2);

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			roleBar.Add(new(roleBar) {TextureActionId = 7498, CooldownActionId = 7498, StatusId = 1232, MaxStatusDuration = 3}, 1); // Third Eye
		}

		private static bool CanUseHiganbana() => JobsHelper.GetPower(JobsHelper.PowerType.Sen).Item1 == 1;
	}
}