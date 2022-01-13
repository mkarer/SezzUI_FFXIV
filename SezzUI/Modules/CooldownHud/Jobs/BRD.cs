namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class BRD : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.BRD;

        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);

            hud.RegisterCooldown(3562); // Sidewinder
        }
    }
}
