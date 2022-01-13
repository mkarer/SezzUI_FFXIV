namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class NIN : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.NIN;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
