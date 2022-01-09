using System.Numerics;

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

			switch (DelvUI.Helpers.JobsHelper.RoleForJob(JobId))
			{
				case DelvUI.Helpers.JobRoles.Tank:
					using (Bar bar = new(hud))
					{
						bar.IconSize = new Vector2(38, 26);
						bar.Add(new Icon(bar) { TextureActionId = 7540, CooldownActionId = 7540, StatusId = 2, MaxStatusDuration = 5, StatusTarget = Enums.Unit.Target, IconClipOffset = 0.5f }); // Low Blow
						bar.Add(new Icon(bar) { TextureActionId = 7538, CooldownActionId = 7538, IconClipOffset = -0.9f }); // Interject
						bar.Add(new Icon(bar) { TextureActionId = 7548, CooldownActionId = 7548, StatusId = 1209, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player }); // Arm's Length
						hud.AddBar(bar);
					}
					break;

				case DelvUI.Helpers.JobRoles.Healer:
					using (Bar bar = new(hud))
					{
						bar.IconSize = new Vector2(38, 26);
						bar.Add(new Icon(bar) { TextureActionId = 7561, CooldownActionId = 7561, StatusId = 167, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player }); // Swiftcast
						bar.Add(new Icon(bar) { TextureActionId = 7562, CooldownActionId = 7562, StatusId = 1204, MaxStatusDuration = 21, StatusTarget = Enums.Unit.Player }); // Lucid Dreaming
						bar.Add(new Icon(bar) { TextureActionId = 7571, CooldownActionId = 7571, RequiresCombat = true }); // Rescue
						bar.Add(new Icon(bar) { TextureActionId = 7559, CooldownActionId = 7559, StatusId = 160, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player, IconClipOffset = 0.8f }); // Surecast
						hud.AddBar(bar);
					}
					break;

				case DelvUI.Helpers.JobRoles.DPSMelee:
					using (Bar bar = new(hud))
					{
						bar.IconSize = new Vector2(38, 26);
						bar.Add(new Icon(bar) { TextureActionId = 7863, CooldownActionId = 7863, StatusId = 2, MaxStatusDuration = 3, StatusTarget = Enums.Unit.Target, IconClipOffset = 0.7f }); // Leg Sweep
						bar.Add(new Icon(bar) { TextureActionId = 7542, CooldownActionId = 7542, StatusId = 84, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, IconClipOffset = 0.7f }); // Bloodbath
						bar.Add(new Icon(bar) { TextureActionId = 7541, CooldownActionId = 7541 }); // Second Wind
						bar.Add(new Icon(bar) { TextureActionId = 7548, CooldownActionId = 7548, StatusId = 1209, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player }); // Arm's Length
						hud.AddBar(bar);
					}
					break;

				case DelvUI.Helpers.JobRoles.DPSRanged:
					using (Bar bar = new(hud))
					{
						bar.IconSize = new Vector2(38, 26);
						bar.Add(new Icon(bar) { TextureActionId = 7551, CooldownActionId = 7551, IconClipOffset = -0.7f }); // Head Gaze
						bar.Add(new Icon(bar) { TextureActionId = 7541, CooldownActionId = 7541 }); // Second Wind
						bar.Add(new Icon(bar) { TextureActionId = 7548, CooldownActionId = 7548, StatusId = 1209, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player }); // Arm's Length
						hud.AddBar(bar);
					}
					break;

				case DelvUI.Helpers.JobRoles.DPSCaster:
					using (Bar bar = new(hud))
					{
						bar.IconSize = new Vector2(38, 26);
						bar.Add(new Icon(bar) { TextureActionId = 7561, CooldownActionId = 7561, StatusId = 167, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player }); // Swiftcast
						bar.Add(new Icon(bar) { TextureActionId = 7559, CooldownActionId = 7559, StatusId = 160, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player, IconClipOffset = 0.8f }); // Surecast
						hud.AddBar(bar);
					}
					break;
			}

			// --------------------------------------------------------------------------------
			// Generic Alerts
			// --------------------------------------------------------------------------------

			// Extreme Caution
			hud.AddAlert(new AuraAlert
			{
				StatusIds = new[] { (uint)1132, (uint)1269 },
				StatusTarget = Enums.Unit.Any,
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\stop.png",
				Size = new Vector2(128, 128) * 0.7f,
				Position = new Vector2(0, 0)
			});

		}
	}
}
