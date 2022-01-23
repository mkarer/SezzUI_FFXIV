using DelvUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class RDM : BasePreset
	{
		public override uint JobId => JobIDs.RDM;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
			
			hud.RegisterCooldown(7517); // Fleche
			hud.RegisterCooldown(7519); // Contre Sixte
			hud.RegisterCooldown(16527); // Engagement
		}
	}
}