namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class BRD : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.BRD;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
