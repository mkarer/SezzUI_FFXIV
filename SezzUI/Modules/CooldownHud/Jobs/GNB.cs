using SezzUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class GNB : BasePreset
	{
		public override uint JobId => JobIDs.GNB;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);

			hud.RegisterCooldown(16144); // Danger Zone
			hud.RegisterCooldown(16153); // Sonic Break
			hud.RegisterCooldown(16146); // Gnashing Fang
			hud.RegisterCooldown(16159); // Bow Shot
			hud.RegisterCooldown(25760); // Double Down
		}
	}
}