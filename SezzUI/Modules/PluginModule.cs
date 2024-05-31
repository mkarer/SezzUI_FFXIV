using System.Collections.Generic;
using SezzUI.Configuration;
using SezzUI.Hooking;
using SezzUI.Interface;

namespace SezzUI.Modules;

/// <summary>
///     Plugin module that also provides a configuration and might have draggable UI elements.
/// </summary>
public abstract class PluginModule : BaseModule
{
	protected PluginConfigObject _config;
	public PluginConfigObject GetConfig() => _config;

	public readonly List<DraggableHudElement> DraggableElements;

	protected PluginModule(PluginConfigObject config)
	{
		Logger.SetPrefix($"PluginModule:{GetType().Name}");
		DraggableElements = new();
		_config = config;
	}

	protected new void Dispose(bool disposing)
	{
		if (!disposing || (this as IPluginDisposable).IsDisposed)
		{
			return;
		}

		(this as IPluginComponent).Disable();
		(this as IHookAccessor)?.DisposeHooks();
		DraggableElements.Clear();
		OnDispose();
		(this as IPluginDisposable).IsDisposed = true;
	}
}