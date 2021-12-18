namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class MNK : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.MNK;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
