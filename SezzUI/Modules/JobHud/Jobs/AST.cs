namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class AST : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.AST;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 3599, StatusActionId = 3599, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Target, Level = 4 }); // Combust
                hud.AddBar(bar);
            }

            base.Configure(hud);
        }
    }
}
