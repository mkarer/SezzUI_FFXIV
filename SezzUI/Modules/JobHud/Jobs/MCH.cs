using System.Linq;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class MCH : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.MCH;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new())
            {
                bar.Add(new Icon(bar) { TextureActionId = 2876, CooldownActionId = 2876, StatusId = 851, MaxStatusDuration = 5, StatusTarget = Enums.Unit.Player, Level = 10 }); // Reassemble
                bar.Add(new Icon(bar) { TextureActionId = 7414, CooldownActionId = 7414, Level = 66 }); // Barrel Stabilizer
                bar.Add(new Icon(bar) { TextureActionId = 2864, CooldownActionId = 2864, RequiredPowerAmount = 60, RequiredPowerType = Helpers.JobsHelper.PowerType.Battery, Level = 66 }); // Rook Autoturret/Automation Queen
                bar.Add(new Icon(bar) { TextureActionId = 2878, CooldownActionId = 2878, StatusId = 861, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Target, Level = 45 }); // Wildfire
                hud.AddBar(bar);
            }

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 16889, CooldownActionId = 16889, StatusId = 1951, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 56 }, 1); // Tactician
        }
    }
}
