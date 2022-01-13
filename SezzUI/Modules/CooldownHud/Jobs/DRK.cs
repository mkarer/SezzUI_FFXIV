namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class DRK : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.DRK;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
