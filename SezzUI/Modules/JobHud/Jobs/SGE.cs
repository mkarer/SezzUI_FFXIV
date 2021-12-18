namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SGE : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SGE;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
