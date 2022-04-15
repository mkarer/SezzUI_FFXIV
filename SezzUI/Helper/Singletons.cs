using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SezzUI.Logging;
using SezzUI.Modules;

namespace SezzUI.Helper
{
	public static class Singletons
	{
		internal static readonly Dictionary<Type, Func<object>> TypeInitializers = new();
		internal static readonly ConcurrentDictionary<Type, short> DisposePriority = new();
		internal static PluginLogger Logger;

		private static readonly ConcurrentDictionary<Type, object> _activeInstances = new();

		static Singletons()
		{
			Logger = new("Singletons");
		}

		public static T Get<T>()
		{
			return (T) _activeInstances.GetOrAdd(typeof(T), objectType =>
			{
				object newInstance;
				if (TypeInitializers.TryGetValue(objectType, out Func<object>? initializer))
				{
					Logger.Debug($"Initializing new instance of type {objectType}");
					newInstance = initializer();
				}
				else
				{
					throw new($"No initializer found for Type {objectType.FullName}");
				}

				if (newInstance is null or not T)
				{
					throw new($"Received invalid result from initializer for type {objectType.FullName}");
				}

				return newInstance;
			});
		}

		public static void Register(object newSingleton, short disposeOrder = default)
		{
			Type type = newSingleton.GetType();
			if (!_activeInstances.TryAdd(type, newSingleton))
			{
				throw new($"Failed to register new singleton for type {type}");
			}

			if (disposeOrder != default)
			{
				DisposePriority[type] = disposeOrder;
			}
		}

		public static bool IsRegistered<T>() => _activeInstances.Values.OfType<T>().Any();

		public static void Dispose()
		{
			Dispose<IPluginDisposable>();
			_activeInstances.Clear();
			DisposePriority.Clear();
		}

		public static void Dispose<T>() where T : IPluginDisposable
		{
			foreach (T component in _activeInstances.Values.OfType<T>().Where(component => !component.IsDisposed).OrderBy(component => DisposePriority.GetValueOrDefault(component.GetType())))
			{
				if (component.IsDisposed)
				{
					continue;
				}

				Type type = component.GetType();
#if DEBUG
				Logger.Debug($"Disposing {type} with priority {DisposePriority.GetValueOrDefault(component.GetType())}");
#endif
				component.Dispose();

				if (!_activeInstances.TryRemove(type, out _))
				{
					Logger.Error($"Failed to remove disposed {type} from active instances");
				}

				DisposePriority.TryRemove(type, out _);
			}
		}
	}
}