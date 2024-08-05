using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

public sealed class DRK : BasePreset
{
	public override uint JobId => JobIDs.DRK;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);
		
		hud.RegisterCooldown(3641); // Abyssal Drain
		hud.RegisterCooldown(16472); // Living Shadow
		hud.RegisterCooldown(3639); // Salted Earth
	}
}