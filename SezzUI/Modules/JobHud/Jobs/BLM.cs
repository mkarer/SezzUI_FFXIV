using System.Numerics;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class BLM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.BLM;
        
        public override void Configure(JobHud hud)
        {
            byte jobLevel = Service.ClientState.LocalPlayer?.Level ?? 0;

            using (Bar bar = new())
            {
                bar.Add(new Icon(bar) { TextureActionId = 144, StatusActionId = 144, MaxStatusDuration = jobLevel >= 45 ? 30 : jobLevel > 26 ? 18 : 21, StatusTarget = Enums.Unit.Target, GlowBorderStatusId = 164, Level = 4 }); // Thunder
                hud.AddBar(bar);
            }

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
