namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class AST : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.AST;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new())
            {
                bar.Add(new Icon(bar) { TextureActionId = 3599, StatusActionId = 838, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Target, Level = 4 }); // Combust
                hud.AddBar(bar);
            }

            base.Configure(hud);
        }
    }
}
