namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class PLD : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.PLD;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
