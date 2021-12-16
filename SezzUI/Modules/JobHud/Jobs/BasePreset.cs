namespace SezzUI.Modules.JobHud
{
	public abstract class BasePreset
	{
		public virtual uint JobId => 0;
		public abstract void Configure(JobHud hud);
	}
}
