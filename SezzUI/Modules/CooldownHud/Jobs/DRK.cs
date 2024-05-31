using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

public sealed class DRK : BasePreset
{
	public override uint JobId => JobIDs.DRK;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);
	}
}