namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class GNB : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.GNB;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
