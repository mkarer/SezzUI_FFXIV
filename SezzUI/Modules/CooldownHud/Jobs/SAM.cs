namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class SAM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SAM;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
