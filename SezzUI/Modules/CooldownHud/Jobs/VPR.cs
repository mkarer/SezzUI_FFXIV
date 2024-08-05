using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

// ReSharper disable once InconsistentNaming
public sealed class VPR : BasePreset
{
	public override uint JobId => JobIDs.VPR;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);

		hud.RegisterCooldown(34646); // Slither
	}
}