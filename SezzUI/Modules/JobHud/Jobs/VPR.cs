using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs;

public sealed class VPR : BasePreset
{
	public override uint JobId => JobIDs.VPR;

	public override void Configure(JobHud hud)
	{
		base.Configure(hud);
	}
}