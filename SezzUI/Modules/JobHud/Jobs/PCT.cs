using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class PCT : BasePreset
{
	public override uint JobId => JobIDs.PCT;

	public override void Configure(JobHud hud)
	{
		Bar bar1 = new(hud);
		bar1.Add(new(bar1) {TextureActionId = 39574, RequiredPowerAmount = 50, RequiredPowerType = JobsHelper.PowerType.Palette, GlowBorderUsable = true}); // Subtractive Palette
		hud.AddBar(bar1);

		base.Configure(hud);
	}
}