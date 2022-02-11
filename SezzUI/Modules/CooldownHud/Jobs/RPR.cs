using SezzUI.Helper;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class RPR : BasePreset
	{
		public override uint JobId => JobIDs.RPR;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
		}
	}
}