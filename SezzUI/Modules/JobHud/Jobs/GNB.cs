using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class GNB : BasePreset
	{
		public override uint JobId => JobIDs.GNB;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 16138, CooldownActionId = 16138, StatusId = 1831, MaxStatusDuration = 20}); // No Mercy
			bar1.Add(new(bar1) {TextureActionId = 16154, CooldownActionId = 16154}); // Rough Divide
			bar1.Add(new(bar1) {TextureActionId = 16164, CooldownActionId = 16164}); // Bloodfest
			bar1.Add(new(bar1) {TextureActionId = 16151, CooldownActionId = 16151, StatusId = 1835, MaxStatusDuration = 18}); // Aurora
			bar1.Add(new(bar1) {TextureActionId = 16160, CooldownActionId = 16160, StatusId = 1839, MaxStatusDuration = 15}); // Heart of Light
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20}); // Rampart
			bar2.Add(new(bar2) {TextureActionId = 16140, CooldownActionId = 16140, StatusId = 1832, MaxStatusDuration = 20}); // Camouflage
			bar2.Add(new(bar2) {TextureActionId = 16148, CooldownActionId = 16148, StatusId = 1834, MaxStatusDuration = 15}); // Nebula
			bar2.Add(new(bar2) {TextureActionId = 16161, CooldownActionId = 16161, StatusIds = new[] {(uint) 1840, (uint) 2683}, MaxStatusDurations = new[] {7f, 8f}, GlowBorderStatusId = 1898}); // Heart of Stone
			bar2.Add(new(bar2) {TextureActionId = 16152, CooldownActionId = 16152, StatusId = 1836, MaxStatusDuration = 10}); // Superbolide
			hud.AddBar(bar2);

			base.Configure(hud);
		}
	}
}