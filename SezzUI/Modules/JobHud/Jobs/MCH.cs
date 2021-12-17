namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class MCH : BasePreset
    {
        public override uint JobId => 31;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new())
            {
                bar.Add(new Icon(bar) { TextureActionId = 2872, CooldownActionId = 2872, Level = 4 }); // Hot Shot 4+
                bar.Add(new Icon(bar) { TextureActionId = 16500, CooldownActionId = 16500, Level = 4 }); // Air Anchor 76+
                hud.AddBar(bar);
            }
        }
    }
}
