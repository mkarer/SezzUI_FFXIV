using System.Linq;
using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class MNK : BasePreset
{
	public override uint JobId => JobIDs.MNK;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 69, CooldownActionId = 69, StatusId = 110, StacksStatusId = 110, MaxStatusDuration = 20, RequiresCombat = true}); // Perfect Balance
		bar1.Add(new(bar1) {TextureActionId = 7395, CooldownActionId = 7395, StatusId = 1181, MaxStatusDuration = 20}); // Riddle of Fire
		bar1.Add(new(bar1) {TextureActionId = 25766, CooldownActionId = 25766, StatusId = 2687, MaxStatusDuration = 15}); // Riddle of Wind
		bar1.Add(new(bar1) {TextureActionId = 7396, CooldownActionId = 7396, StatusIds = new[] {1182u, 1185u}, MaxStatusDuration = 20}); // Brotherhood
		hud.AddBar(bar1);

		base.Configure(hud);

		Bar roleBar = hud.Bars.Last();
		roleBar.Add(new(roleBar) {TextureActionId = 65, CooldownActionId = 65, StatusId = 102, MaxStatusDuration = 15, StatusSourcePlayer = false}, 1); // Mantra
		roleBar.Add(new(roleBar) {TextureActionId = 7394, CooldownActionId = 7394, StatusId = 1179, MaxStatusDuration = 10}, 1); // Riddle of Earth
	}
}