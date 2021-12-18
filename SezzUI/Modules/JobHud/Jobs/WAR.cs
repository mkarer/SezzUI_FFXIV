namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class WAR : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.WAR;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
