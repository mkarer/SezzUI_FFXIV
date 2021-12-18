namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class NIN : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.NIN;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
