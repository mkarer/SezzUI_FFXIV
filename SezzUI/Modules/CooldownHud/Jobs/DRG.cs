namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class DRG : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.DRG;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
