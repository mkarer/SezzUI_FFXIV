namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class SGE : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SGE;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
