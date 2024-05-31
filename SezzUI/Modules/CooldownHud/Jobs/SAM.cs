using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

public sealed class SAM : BasePreset
{
	public override uint JobId => JobIDs.SAM;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);

		hud.RegisterCooldown(16486); // Kaeshi: Setsugekka
		hud.RegisterCooldown(16481); // Hissatsu: Senei
	}
}