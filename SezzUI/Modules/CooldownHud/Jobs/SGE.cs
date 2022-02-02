using SezzUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class SGE : BasePreset
	{
		public override uint JobId => JobIDs.SGE;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);

			hud.RegisterCooldown(24303); // Taurochole
			hud.RegisterCooldown(24301); // Pepsis
			hud.RegisterCooldown(24299); // Ixochole
			hud.RegisterCooldown(24288); // Physis
			hud.RegisterCooldown(24300); // Zoe
			hud.RegisterCooldown(24295); // Icarus
		}
	}
}