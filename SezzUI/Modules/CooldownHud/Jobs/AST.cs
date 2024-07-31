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
		hud.RegisterCooldown(16556); // Celestial Intersection
		hud.RegisterCooldown(16553); // Celestial Opposition
		hud.RegisterCooldown(16557); // Horoscope
		hud.RegisterCooldown(16559); // Neutral Sect
	}
}