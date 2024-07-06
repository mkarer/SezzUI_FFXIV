using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

// ReSharper disable once InconsistentNaming
public sealed class PCT : BasePreset
{
	public override uint JobId => JobIDs.PCT;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);

		hud.RegisterCooldown(34684); // Smudge
	}
}