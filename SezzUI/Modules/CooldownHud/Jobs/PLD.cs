namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class PLD : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.PLD;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
