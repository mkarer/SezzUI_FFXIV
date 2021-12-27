using System.Numerics;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SGE : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SGE;
        
        public override void Configure(JobHud hud)
        {
            // Kardia
            using (AuraAlert aa = new())
            {
                aa.StatusId = 2604;
                aa.InvertCheck = true;
                aa.Size = new Vector2(48, 48);
                aa.Position = new Vector2(0, -80);
                aa.Level = 4;
                aa.BorderSize = 1;
                aa.UseActionIcon(24285);
                hud.AddAlert(aa);
            };

            base.Configure(hud);
        }
    }
}
