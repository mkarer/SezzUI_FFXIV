using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class SMN : BasePreset
	{
		public override uint JobId => JobIDs.SMN;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
		}
	}
}