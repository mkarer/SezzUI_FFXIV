namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class DNC : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.DNC;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
