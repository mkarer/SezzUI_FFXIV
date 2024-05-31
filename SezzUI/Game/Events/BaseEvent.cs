using SezzUI.Modules;

namespace SezzUI.Game.Events;

internal abstract class BaseEvent : BaseModule
{
	protected BaseEvent()
	{
		Logger.SetPrefix($"Event:{GetType().Name}");
	}
}