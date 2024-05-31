using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

public sealed class MNK : BasePreset
{
	public override uint JobId => JobIDs.MNK;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);

		hud.RegisterCooldown(16475); // Anatman
	}
}