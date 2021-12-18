namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SCH : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SCH;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
