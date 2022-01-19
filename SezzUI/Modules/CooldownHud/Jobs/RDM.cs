using DelvUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class RDM : BasePreset
	{
		public override uint JobId => JobIDs.RDM;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
		}
	}
}