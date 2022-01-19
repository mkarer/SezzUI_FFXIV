using DelvUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class AST : BasePreset
	{
		public override uint JobId => JobIDs.AST;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
		}
	}
}