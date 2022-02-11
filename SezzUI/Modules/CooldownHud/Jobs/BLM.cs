using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class BLM : BasePreset
	{
		public override uint JobId => JobIDs.BLM;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
		}
	}
}