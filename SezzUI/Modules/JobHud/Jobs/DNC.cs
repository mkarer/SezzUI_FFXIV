using System.Linq;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class DNC : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.DNC;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 15997, CooldownActionId = 15997, StatusId = 1818, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 15 }); // Standard Step
                bar.Add(new Icon(bar) { TextureActionId = 15998, CooldownActionId = 15998, StatusId = 1819, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 70 }); // Technical Step
                bar.Add(new Icon(bar) { TextureActionId = 16010, CooldownActionId = 16010, Level = 50 }); // En Avant
                bar.Add(new Icon(bar) { TextureActionId = 16011, CooldownActionId = 16011, StatusId = 1825, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 62 }); // Devilment
                hud.AddBar(bar);
            }

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 16012, CooldownActionId = 16012, StatusId = 1826, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 60 }, 1); // Shield Samba
        }
    }
}
