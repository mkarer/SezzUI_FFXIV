namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class MNK : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.MNK;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
