using System.Linq;
using DelvUI.Helpers;
using SezzUI.Enums;
using JobsHelper = SezzUI.Helpers.JobsHelper;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class MCH : BasePreset
	{
		public override uint JobId => JobIDs.MCH;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 2876, CooldownActionId = 2876, StatusId = 851, MaxStatusDuration = 5}); // Reassemble
			bar1.Add(new(bar1) {TextureActionId = 7414, CooldownActionId = 7414, RequiresCombat = true}); // Barrel Stabilizer
			bar1.Add(new(bar1) {TextureActionId = 2864, CooldownActionId = 2864, RequiredPowerAmount = 60, RequiredPowerType = JobsHelper.PowerType.Battery}); // Rook Autoturret/Automation Queen
			bar1.Add(new(bar1) {TextureActionId = 2878, CooldownActionId = 2878, StatusId = 861, MaxStatusDuration = 10, StatusTarget = Unit.Target}); // Wildfire
			hud.AddBar(bar1);

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			roleBar.Add(new(roleBar) {TextureActionId = 16889, CooldownActionId = 16889, StatusId = 1951, MaxStatusDuration = 15}, 1); // Tactician
		}
	}
}