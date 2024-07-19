using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs;

public sealed class AST : BasePreset
{
	public override uint JobId => JobIDs.AST;

	public override void Configure(CooldownHud hud)
	{
		base.Configure(hud);
		
		hud.RegisterCooldown(7439); // Earthly Star
		hud.RegisterCooldown(16552); // Divination
		hud.RegisterCooldown(3612); // Synastry
	}
}