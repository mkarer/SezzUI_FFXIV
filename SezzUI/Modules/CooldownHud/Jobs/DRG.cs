using DelvUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class DRG : BasePreset
	{
		public override uint JobId => JobIDs.DRG;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);

			hud.RegisterCooldown(94); // Elusive Jump
			hud.RegisterCooldown(95); // Spineshatter Dive
			hud.RegisterCooldown(92); // Jump
			hud.RegisterCooldown(16480); // Stardriver
		}
	}
}