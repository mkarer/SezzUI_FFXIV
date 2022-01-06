using System;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class DRK : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.DRK;

        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 3625, CooldownActionId = 3625, StatusId = 742, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 35 }); // Blood Weapon
                bar.Add(new Icon(bar) { TextureActionId = 7390, CooldownActionId = 7390, StatusId = 1972, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, GlowBorderStatusId = 1972, Features = IconFeatures.GlowIgnoresState, Level = 68 }); // Delirium
                bar.Add(new Icon(bar) { TextureActionId = 3640, CooldownActionId = 3640, Level = 56 }); // Plunge
                bar.Add(new Icon(bar) { TextureActionId = 16472, CooldownActionId = 16472, RequiredPowerType = Helpers.JobsHelper.PowerType.Blood, RequiredPowerAmount = 50, CustomDuration = GetLivingShadowDuration, Level = 80 }); // Living Shadow
                bar.Add(new Icon(bar) { TextureActionId = 7393, CooldownActionId = 7393, StatusId = 1178, MaxStatusDuration = 7, StatusTarget = Enums.Unit.Player, RequiredPowerType = Helpers.JobsHelper.PowerType.MP, RequiredPowerAmount = 3000, Level = 70 }); // The Blackest Night
                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 8 }); // Rampart
                bar.Add(new Icon(bar) { TextureActionId = 3636, CooldownActionId = 3636, StatusActionId = 3636, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 38 }); // Shadow Wall
                bar.Add(new Icon(bar) { TextureActionId = 16471, CooldownActionId = 16471, StatusActionId = 16471, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 76 }); // Dark Missionary
                bar.Add(new Icon(bar) { TextureActionId = 3634, CooldownActionId = 3634, StatusActionId = 3634, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 45 }); // Dark Mind
                bar.Add(new Icon(bar) { TextureActionId = 3638, CooldownActionId = 3638, StatusActionId = 3638, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 50 }); // Living Dead
                hud.AddBar(bar);
            }
         
            base.Configure(hud);
        }

        public static (float, float) GetLivingShadowDuration()
        {
            DRKGauge gauge = Plugin.JobGauges.Get<DRKGauge>();
            if (gauge != null && gauge.ShadowTimeRemaining != 0)
            {
                return (gauge.ShadowTimeRemaining / 1000f, 24);
            }
            else
            {
                return (0, 0);
            }
        }
    }
}
