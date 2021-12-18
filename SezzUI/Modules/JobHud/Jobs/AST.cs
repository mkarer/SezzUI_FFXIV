namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class AST : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.AST;
        
        public override void Configure(JobHud hud)
        {
            base.Configure(hud);
        }
    }
}
