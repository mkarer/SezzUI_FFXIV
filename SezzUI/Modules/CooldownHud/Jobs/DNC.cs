namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class DNC : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.DNC;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}