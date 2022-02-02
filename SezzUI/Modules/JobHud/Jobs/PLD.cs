using SezzUI.Enums;
using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class PLD : BasePreset
	{
		public override uint JobId => JobIDs.PLD;

		public override void Configure(JobHud hud)
		{
			byte jobLevel = Plugin.ClientState.LocalPlayer?.Level ?? 0;

			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 20, CooldownActionId = 20, StatusId = 76, MaxStatusDuration = 25}); // Fight of Flight
			bar1.Add(new(bar1) {TextureActionId = 3538, StatusId = 725, MaxStatusDuration = 21, StatusTarget = Unit.Target}); // Goring Blade
			bar1.Add(new(bar1) {TextureActionId = 16461, CooldownActionId = 16461}); // Intervene
			bar1.Add(new(bar1) {TextureActionId = 3542, CooldownActionId = 3542, StatusId = 1856, MaxStatusDuration = jobLevel >= 74 ? 6 : 4, RequiredPowerType = JobsHelper.PowerType.Oath, RequiredPowerAmount = 50, GlowBorderUsable = true}); // Sheltron
			bar1.Add(new(bar1) {TextureActionId = 7382, CooldownActionId = 7382, StatusId = 1174, MaxStatusDuration = 6, StatusTarget = Unit.Target, GlowBorderStatusIds = new[] {1191u, 74u}, RequiredPowerType = JobsHelper.PowerType.Oath, RequiredPowerAmount = 50}); // Intervention
			bar1.Add(new(bar1) {TextureActionId = 7383, CooldownActionId = 7383, StatusId = 1368, MaxStatusDuration = 30}); // Requiescat
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20}); // Rampart
			bar2.Add(new(bar2) {TextureActionId = 17, CooldownActionId = 17, StatusId = 74, MaxStatusDuration = 15}); // Sentinel
			bar2.Add(new(bar2) {TextureActionId = 7385, CooldownActionId = 7385, StatusId = 1175, MaxStatusDuration = 18}); // Passage of Arms
			bar2.Add(new(bar2) {TextureActionId = 3540, CooldownActionId = 3540, StatusId = 726, MaxStatusDuration = 30, GlowBorderStatusId = 726, Features = IconFeatures.GlowIgnoresState}); // Divine Veil
			bar2.Add(new(bar2) {TextureActionId = 30, CooldownActionId = 30, StatusId = 82, MaxStatusDuration = 10}); // Hallowed Ground
			hud.AddBar(bar2);

			// Confiteor
			hud.AddAlert(new()
			{
				StatusId = 1368,
				ExactStacks = 1,
				Image = "hand_of_light.png",
				Size = new(256, 128),
				Position = new(0, -120),
				Level = 80
			});

			base.Configure(hud);
		}
	}
}