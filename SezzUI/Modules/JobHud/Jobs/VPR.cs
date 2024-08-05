using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class VPR : BasePreset
{
	public override uint JobId => JobIDs.VPR;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 34647, CooldownActionId = 34647, Level = 86}); // Serpent's Ire
		hud.AddBar(bar1);

		base.Configure(hud);
	}
}