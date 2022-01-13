namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class RPR : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.RPR;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
