using DelvUI.Helpers;

namespace SezzUI.Modules.CooldownHud
{
	public abstract class BasePreset
	{
		public virtual uint JobId => 0;

		public virtual void Configure(CooldownHud hud)
		{
			// --------------------------------------------------------------------------------
			// Role Actions
			// --------------------------------------------------------------------------------

			switch (JobsHelper.RoleForJob(JobId))
			{
				case JobRoles.Tank:
					hud.RegisterCooldown(7538, 1); // Interject
					hud.RegisterCooldown(7533); // Provoke
					hud.RegisterCooldown(7535); // Reprisal
					hud.RegisterCooldown(7537); // Shirk
					break;

				case JobRoles.Healer:
					break;

				case JobRoles.DPSMelee:
					break;

				case JobRoles.DPSRanged:
					hud.RegisterCooldown(7551, 1); // Head Graze
					break;

				case JobRoles.DPSCaster:
					break;
			}

			// --------------------------------------------------------------------------------
			// General Actions
			// --------------------------------------------------------------------------------

			hud.RegisterCooldown(4); // Sprint
			hud.RegisterCooldown(8); // Return
		}
	}
}