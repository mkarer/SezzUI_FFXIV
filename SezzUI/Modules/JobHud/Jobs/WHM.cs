using DelvUI.Helpers;
using SezzUI.Enums;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class WHM : BasePreset
	{
		public override uint JobId => JobIDs.WHM;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 121, StatusActionId = 121, MaxStatusDuration = 18, StatusTarget = Unit.Target}); // Aero
			bar1.Add(new(bar1) {TextureActionId = 7430, CooldownActionId = 7430, StatusId = 1217, MaxStatusDuration = 12}); // Thin Air
			bar1.Add(new(bar1) {TextureActionId = 136, CooldownActionId = 136, StatusId = 157, MaxStatusDuration = 15}); // Presence of Mind
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 140, CooldownActionId = 140, Level = 50}); // Benediction
			bar2.Add(new(bar2) {TextureActionId = 7433, CooldownActionId = 7433, StatusId = 1219, MaxStatusDuration = 10}); // Plenary Indulgence
			hud.AddBar(bar2);

			base.Configure(hud);
		}
	}
}