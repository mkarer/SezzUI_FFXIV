using SezzUI.Helpers;

namespace SezzUI.Modules.CooldownHud.Jobs
{
	public sealed class WHM : BasePreset
	{
		public override uint JobId => JobIDs.WHM;

		public override void Configure(CooldownHud hud)
		{
			base.Configure(hud);
		}
	}
}