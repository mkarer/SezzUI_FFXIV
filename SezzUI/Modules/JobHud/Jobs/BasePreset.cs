using System.Numerics;
using DelvUI.Helpers;
using SezzUI.Enums;

namespace SezzUI.Modules.JobHud
{
	public abstract class BasePreset
	{
		public virtual uint JobId => 0;

		public virtual void Configure(JobHud hud)
		{
			// --------------------------------------------------------------------------------
			// Role Actions
			// --------------------------------------------------------------------------------

			switch (JobsHelper.RoleForJob(JobId))
			{
				case JobRoles.Tank:
					Bar barTankRole = new(hud);
					barTankRole.IconSize = new(38, 26);
					barTankRole.Add(new(barTankRole) {TextureActionId = 7540, CooldownActionId = 7540, StatusId = 2, MaxStatusDuration = 5, StatusTarget = Unit.Target, IconClipOffset = 0.5f}); // Low Blow
					barTankRole.Add(new(barTankRole) {TextureActionId = 7538, CooldownActionId = 7538, IconClipOffset = -0.9f}); // Interject
					barTankRole.Add(new(barTankRole) {TextureActionId = 7548, CooldownActionId = 7548, StatusId = 1209, MaxStatusDuration = 6}); // Arm's Length
					hud.AddBar(barTankRole);
					break;

				case JobRoles.Healer:
					Bar barHealerRole = new(hud);
					barHealerRole.IconSize = new(38, 26);
					barHealerRole.Add(new(barHealerRole) {TextureActionId = 7561, CooldownActionId = 7561, StatusId = 167, MaxStatusDuration = 10}); // Swiftcast
					barHealerRole.Add(new(barHealerRole) {TextureActionId = 7562, CooldownActionId = 7562, StatusId = 1204, MaxStatusDuration = 21}); // Lucid Dreaming
					barHealerRole.Add(new(barHealerRole) {TextureActionId = 7571, CooldownActionId = 7571, RequiresCombat = true}); // Rescue
					barHealerRole.Add(new(barHealerRole) {TextureActionId = 7559, CooldownActionId = 7559, StatusId = 160, MaxStatusDuration = 6, IconClipOffset = 0.8f}); // Surecast
					hud.AddBar(barHealerRole);
					break;

				case JobRoles.DPSMelee:
					Bar barDPSMeleeRole = new(hud);
					barDPSMeleeRole.IconSize = new(38, 26);
					barDPSMeleeRole.Add(new(barDPSMeleeRole) {TextureActionId = 7863, CooldownActionId = 7863, StatusId = 2, MaxStatusDuration = 3, StatusTarget = Unit.Target, IconClipOffset = 0.7f}); // Leg Sweep
					barDPSMeleeRole.Add(new(barDPSMeleeRole) {TextureActionId = 7542, CooldownActionId = 7542, StatusId = 84, MaxStatusDuration = 20, IconClipOffset = 0.7f}); // Bloodbath
					barDPSMeleeRole.Add(new(barDPSMeleeRole) {TextureActionId = 7541, CooldownActionId = 7541}); // Second Wind
					barDPSMeleeRole.Add(new(barDPSMeleeRole) {TextureActionId = 7548, CooldownActionId = 7548, StatusId = 1209, MaxStatusDuration = 6}); // Arm's Length
					hud.AddBar(barDPSMeleeRole);
					break;

				case JobRoles.DPSRanged:
					Bar barDPSRangedRole = new(hud);
					barDPSRangedRole.IconSize = new(38, 26);
					barDPSRangedRole.Add(new(barDPSRangedRole) {TextureActionId = 7551, CooldownActionId = 7551, IconClipOffset = -0.7f}); // Head Graze
					barDPSRangedRole.Add(new(barDPSRangedRole) {TextureActionId = 7541, CooldownActionId = 7541}); // Second Wind
					barDPSRangedRole.Add(new(barDPSRangedRole) {TextureActionId = 7548, CooldownActionId = 7548, StatusId = 1209, MaxStatusDuration = 6}); // Arm's Length
					hud.AddBar(barDPSRangedRole);
					break;

				case JobRoles.DPSCaster:
					Bar barDPSCasterRole = new(hud);
					barDPSCasterRole.IconSize = new(38, 26);
					barDPSCasterRole.Add(new(barDPSCasterRole) {TextureActionId = 7561, CooldownActionId = 7561, StatusId = 167, MaxStatusDuration = 10}); // Swiftcast
					barDPSCasterRole.Add(new(barDPSCasterRole) {TextureActionId = 7559, CooldownActionId = 7559, StatusId = 160, MaxStatusDuration = 6, IconClipOffset = 0.8f}); // Surecast
					hud.AddBar(barDPSCasterRole);
					break;
			}

			// --------------------------------------------------------------------------------
			// Generic Alerts
			// --------------------------------------------------------------------------------

			// Extreme Caution
			hud.AddAlert(new()
			{
				StatusIds = new[] {(uint) 1132, (uint) 1269},
				StatusTarget = Unit.Any,
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\stop.png",
				Size = new Vector2(128, 128) * 0.7f,
				Position = new(0, 0)
			});
		}
	}
}