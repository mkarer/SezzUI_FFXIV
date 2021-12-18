namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SAM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SAM;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
