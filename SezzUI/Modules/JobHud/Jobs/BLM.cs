using System.Numerics;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class BLM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.BLM;
        
        public override void Configure(JobHud hud)
        {
            // Firestarter
            hud.AddAlert(new AuraAlert
            {
                StatusId = 165,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\impact.png",
                Size = new Vector2(256, 128) * 0.8f,
                Position = new Vector2(0, -180),
                MaxDuration = 30
            });

            base.Configure(hud);
        }
    }
}
