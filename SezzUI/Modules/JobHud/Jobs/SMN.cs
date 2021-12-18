namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SMN : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SMN;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
