using DelvUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class WAR : BasePreset
	{
		public override uint JobId => JobIDs.WAR;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
		}
	}
}