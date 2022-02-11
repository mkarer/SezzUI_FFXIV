using System;
using System.Linq;
using System.Reflection;
using SezzUI.Helper;
using SezzUI.Modules;

namespace SezzUI.Game.Events
{
	internal class EventManager : IPluginDisposable
	{
		internal static Game Game => Singletons.Get<Game>();
		internal static Player Player => Singletons.Get<Player>();
		internal static Combat Combat => Singletons.Get<Combat>();
		internal static Cooldown.Cooldown Cooldown => Singletons.Get<Cooldown.Cooldown>();
		internal static DutyFinderQueue DutyFinderQueue => Singletons.Get<DutyFinderQueue>();

		public EventManager()
		{
			try
			{
				foreach (Type eventType in Assembly.GetAssembly(typeof(BaseEvent))!.GetTypes().Where(t => t.BaseType == typeof(BaseEvent)))
				{
					Singletons.TypeInitializers.Add(eventType, () => Activator.CreateInstance(eventType)!);
					Singletons.DisposePriority[eventType] = 45; // Should be higher than EventManager's priority which is not known yet. 
				}
			}
			catch (Exception ex)
			{
				Plugin.Logger.Error($"Failed to register initializers for events: {ex}");
			}
		}

		~EventManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool IPluginDisposable.IsDisposed { get; set; } = false;

		protected void Dispose(bool disposing)
		{
			if (!disposing || (this as IPluginDisposable).IsDisposed)
			{
				return;
			}

			Singletons.Dispose<BaseEvent>();

			(this as IPluginDisposable).IsDisposed = true;
		}
	}
}