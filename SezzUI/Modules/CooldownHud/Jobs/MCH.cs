namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class MCH : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.MCH;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
