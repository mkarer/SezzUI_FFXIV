namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class DRG : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.DRG;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
