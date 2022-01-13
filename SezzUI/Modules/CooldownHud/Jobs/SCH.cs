namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class SCH : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SCH;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
