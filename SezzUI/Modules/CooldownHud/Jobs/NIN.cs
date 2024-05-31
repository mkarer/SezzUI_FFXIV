using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

public sealed class NIN : BasePreset
{
	public override uint JobId => JobIDs.NIN;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);

		hud.RegisterCooldown(2248); // Mug
		hud.RegisterCooldown(2245); // Hide
	}
}