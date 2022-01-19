using DelvUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class MCH : BasePreset
	{
		public override uint JobId => JobIDs.MCH;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);

			hud.RegisterCooldown(2872); // Hot Shot
			hud.RegisterCooldown(16498); // Drill
			hud.RegisterCooldown(25788); // Chain Saw
		}
	}
}