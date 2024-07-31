using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class WHM : BasePreset
{
	public override uint JobId => JobIDs.WHM;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 121, StatusActionId = 121, MaxStatusDuration = 30, StatusTarget = Unit.Target}); // Aero
		bar1.Add(new(bar1) {TextureActionId = 7430, CooldownActionId = 7430, StatusId = 1217, MaxStatusDuration = 12, Level = 58}); // Thin Air
		bar1.Add(new(bar1) {TextureActionId = 37011, CooldownActionId = 37011, StatusId = 3903, MaxStatusDuration = 10, Level = 100}); // Divine Caress
		bar1.Add(new(bar1) {TextureActionId = 136, CooldownActionId = 136, StatusId = 157, MaxStatusDuration = 15}); // Presence of Mind
		hud.AddBar(bar1);

		Bar bar2 = new(hud);
		bar2.Add(new(bar2) {TextureActionId = 16536, CooldownActionId = 16536, StatusId = 1872, MaxStatusDuration = 20, Level = 80}); // Temperance
		bar2.Add(new(bar2) {TextureActionId = 140, CooldownActionId = 140, Level = 50}); // Benediction
		bar2.Add(new(bar2) {TextureActionId = 25861, CooldownActionId = 25861, StatusId = 2708, MaxStatusDuration = 8, Level = 86}); // Aquaveil
		bar2.Add(new(bar2) {TextureActionId = 7433, CooldownActionId = 7433, StatusId = 1219, MaxStatusDuration = 10}); // Plenary Indulgence
		bar2.Add(new(bar2) {TextureActionId = 25862, CooldownActionId = 25862, Level = 90}); // Liturgy of the Bell
		hud.AddBar(bar2);

		base.Configure(hud);
	}
}