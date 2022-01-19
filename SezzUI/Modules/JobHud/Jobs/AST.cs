using DelvUI.Helpers;
using SezzUI.Enums;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class AST : BasePreset
	{
		public override uint JobId => JobIDs.AST;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 3599, StatusActionId = 3599, MaxStatusDuration = 30, StatusTarget = Unit.Target}); // Combust
			hud.AddBar(bar1);

			base.Configure(hud);
		}
	}
}