using System.Linq;
using System.Numerics;
using SezzUI.Enums;
using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class SGE : BasePreset
	{
		public override uint JobId => JobIDs.SGE;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 24293, StatusIds = new[] {2614u, 2615u, 2616u}, MaxStatusDuration = 30, StatusTarget = Unit.Target, RequiredPowerAmount = 400, RequiredPowerType = JobsHelper.PowerType.MP}); // Eukrasian Dosis
			bar1.Add(new(bar1) {TextureActionId = 24298, CooldownActionId = 24298, StatusIds = new[] {2618u, 2938u, 3003u}, MaxStatusDurations = new[] {15f, 15f, 20f}, RequiredPowerType = JobsHelper.PowerType.Addersgall, RequiredPowerAmount = 1}); // Kerachole/Holos
			bar1.Add(new(bar1) {TextureActionId = 24310, CooldownActionId = 24310, StatusIds = new[] {2618u, 2938u, 3003u}, MaxStatusDurations = new[] {15f, 15f, 20f}}); // Holos
			//bar1.Add(new Icon(bar1) { TextureActionId = 24288, CooldownActionId = 24288, StatusActionId = 24288, MaxStatusDuration = 15}); // Physis
			bar1.Add(new(bar1) {TextureActionId = 24305, CooldownActionId = 24305, StatusId = 2612, MaxStatusDuration = 15, StatusTarget = Unit.TargetOrPlayer}); // Haima
			bar1.Add(new(bar1) {TextureActionId = 24311, CooldownActionId = 24311, StatusId = 2613, MaxStatusDuration = 15}); // Panhaima
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 24294, CooldownActionId = 24294, StatusId = 2610, MaxStatusDuration = 15}); // Soteria
			bar2.Add(new(bar2) {TextureActionId = 24295, CooldownActionId = 24295}); // Icarus
			//bar2.Add(new Icon(bar2) { TextureActionId = 24300, CooldownActionId = 24300, StatusId = 2611, MaxStatusDuration = 30}); // Zoe
			bar2.Add(new(bar2) {TextureActionId = 24317, CooldownActionId = 24317, StatusId = 2622, MaxStatusDuration = 10, StatusTarget = Unit.TargetOrPlayer}); // Krasis
			bar2.Add(new(bar2) {TextureActionId = 24289, CooldownActionId = 24289, RequiredPowerAmount = 400, RequiredPowerType = JobsHelper.PowerType.MP}); // Phlegma
			bar2.Add(new(bar2) {TextureActionId = 24318, CooldownActionId = 24318, RequiredPowerAmount = 700, RequiredPowerType = JobsHelper.PowerType.MP}); // Pneuma
			hud.AddBar(bar2);

			// Kardia
			using (AuraAlert aa = new())
			{
				aa.StatusId = 2604;
				aa.InvertCheck = true;
				aa.Size = new(48, 48);
				aa.Position = new(0, -220);
				aa.Level = 4;
				aa.BorderSize = 1;
				aa.UseActionIcon(24285);
				aa.GlowBackdrop = true;
				aa.GlowColor = new(74f / 255f, 137f / 255f, 214f / 255f, 0.5f);
				aa.GlowBackdropSize = 4;
				hud.AddAlert(aa);
			}

			// Eukrasia
			hud.AddAlert(new()
			{
				StatusId = 2606,
				Size = new Vector2(256, 128) * 0.9f,
				Image = "genericarc_05_90.png",
				Position = new(0, -60),
				Color = new(0 / 255f, 221f / 255f, 210f / 255f, 1f),
				Level = 30
			});

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			roleBar.Add(new(roleBar) {TextureActionId = 24309, CooldownActionId = 24309}, 2); // Rhizomata
		}
	}
}