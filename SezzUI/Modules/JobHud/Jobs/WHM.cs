using System.Numerics;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class WHM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.WHM;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 121, StatusActionId = 121, MaxStatusDuration = 18, StatusTarget = Enums.Unit.Target, Level = 4 }); // Aero
                bar.Add(new Icon(bar) { TextureActionId = 7430, CooldownActionId = 7430, StatusId = 1217, MaxStatusDuration = 12, StatusTarget = Enums.Unit.Player, Level = 58 }); // Thin Air
                bar.Add(new Icon(bar) { TextureActionId = 136, CooldownActionId = 136, StatusId = 157, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 30 }); // Presence of Mind
                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 140, CooldownActionId = 140, Level = 50 }); // Benediction
                bar.Add(new Icon(bar) { TextureActionId = 7433, CooldownActionId = 7433, StatusId = 1219, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 70 }); // Plenary Indulgence
                hud.AddBar(bar);
            }

            base.Configure(hud);
        }
    }
}
