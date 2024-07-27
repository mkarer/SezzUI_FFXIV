using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class AST : BasePreset
{
	public override uint JobId => JobIDs.AST;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 3599, StatusActionId = 3599, MaxStatusDuration = 30, StatusTarget = Unit.Target}); // Combust
		bar1.Add(new(bar1) {TextureActionId = 37017, TextureActionAllowCombo = true, CooldownActionId = 37017}); // Astral/Umbral Draw
		bar1.Add(new(bar1) {TextureActionId = 3613, CooldownActionId = 3613, StatusId = 848, MaxStatusDuration = 18, StatusTarget = Unit.Player}); // Collective Unconscious
		bar1.Add(new(bar1) {TextureActionId = 16557, CooldownActionId = 16557, StatusIds = new[] {1890u, 1891u}, MaxStatusDurations = new[] {10f, 30f}, StatusTarget = Unit.Player}); // Horoscope
		bar1.Add(new(bar1) {TextureActionId = 3606, CooldownActionId = 3606, StatusId = 841, MaxStatusDuration = 15}); // Lightspeed
		hud.AddBar(bar1);

		Bar bar2 = new(hud);
		bar2.Add(new(bar2) {TextureActionId = 16556, CooldownActionId = 16556, StatusId = 1889, MaxStatusDuration = 30, StatusTarget = Unit.Target}); // Celestial Intersection
		bar2.Add(new(bar2) {TextureActionId = 16553, CooldownActionId = 16553, StatusId = 1879, MaxStatusDuration = 18, StatusTarget = Unit.Player}); // Celestial Opposition
		bar2.Add(new(bar2) {TextureActionId = 16559, CooldownActionId = 16559, StatusId = 1892, MaxStatusDuration = 20, StatusTarget = Unit.Player}); // Neutral Sect
		bar2.Add(new(bar2) {TextureActionId = 7439, CooldownActionId = 7439, StatusIds = new[] {1224u, 1248u}, MaxStatusDurations = new[] {10f, 10f}, StatusTarget = Unit.Player}); // Earthly Star
		bar2.Add(new(bar2) {TextureActionId = 16552, CooldownActionId = 16552, StatusId = 1878, MaxStatusDuration = 20, StatusTarget = Unit.Player}); // Divination

		hud.AddBar(bar2);

		base.Configure(hud);
	}
}