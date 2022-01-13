namespace SezzUI.Modules.CooldownHud.Jobs
{
    public sealed class AST : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.AST;
        
        public override void Configure(CooldownHud hud)
        {
            base.Configure(hud);
        }
    }
}
