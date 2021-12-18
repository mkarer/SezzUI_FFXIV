namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class RDM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.RDM;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
