using System.Numerics;

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

			switch (DelvUI.Helpers.JobsHelper.RoleForJob(JobId))
			{
				case DelvUI.Helpers.JobRoles.Tank:
                    hud.RegisterCooldown(7538, 1); // Interject
                    hud.RegisterCooldown(7533); // Provoke
                    hud.RegisterCooldown(7535); // Reprisal
                    hud.RegisterCooldown(7537); // Shirk
                    break;

                case DelvUI.Helpers.JobRoles.Healer:
					break;

				case DelvUI.Helpers.JobRoles.DPSMelee:
					break;

				case DelvUI.Helpers.JobRoles.DPSRanged:
                    hud.RegisterCooldown(7551, 1); // Head Graze
                    break;

				case DelvUI.Helpers.JobRoles.DPSCaster:
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
