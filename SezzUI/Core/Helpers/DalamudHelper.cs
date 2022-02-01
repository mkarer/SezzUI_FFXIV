// Stolen from goat's Wotsit
// https://github.com/goaaats/Dalamud.FindAnything/blob/master/Dalamud.FindAnything/DalamudReflector.cs
// https://stackoverflow.com/questions/1565734/is-it-possible-to-set-private-property-via-reflection

using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Plugin;

namespace SezzUI.Helpers
{
	public static class DalamudHelper
	{
		internal static T GetService<T>() => (T) typeof(IDalamudPlugin).Assembly.GetType("Dalamud.Service`1")!.MakeGenericType(typeof(T)).GetMethod("Get", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, null)!;
		internal static object GetService(string name) => typeof(IDalamudPlugin).Assembly.GetType("Dalamud.Service`1")!.MakeGenericType(typeof(IDalamudPlugin).Assembly.GetType(name)!).GetMethod("Get", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, null)!;

		public struct PluginEntry
		{
			public string Name;
			public bool Enabled;
		}

		public static IReadOnlyList<PluginEntry> Plugins { get; private set; }
		internal static PluginLogger Logger;

		public static void RefreshPlugins()
		{
			object pluginManager = GetService("Dalamud.Plugin.Internal.PluginManager");
			IEnumerable<object> pluginList = pluginManager.GetPropertyValue<IEnumerable<object>>("InstalledPlugins"); // LocalPlugin

			List<PluginEntry> list = new();
			foreach (object plugin in pluginList)
			{
				if (plugin.GetType().GetProperty("DalamudInterface", BindingFlags.Public | BindingFlags.Instance)!.GetValue(plugin) == null)
				{
					continue;
				}

				try
				{
					string name = plugin.GetPropertyValue<string>("Name");
					bool loaded = plugin.GetPropertyValue<bool>("IsLoaded");
					if (loaded)
					{
						bool enabled = true; // Assume that all unsupported plugins are enabled

						switch (name)
						{
							case "TextAdvance":
								enabled = plugin.GetFieldValue<IDalamudPlugin>("instance").GetFieldValue<bool>("Enabled");
								//Logger.Debug("RefreshPlugins", $"Plugin: {name} Enabled: {enabled}");
								break;
						}

						//Logger.Debug("RefreshPlugins", $"Plugin: {name} Enabled: {enabled}");
						PluginEntry entry = new()
						{
							Name = name,
							Enabled = enabled
						};

						list.Add(entry);
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "RefreshPlugins", $"Error: {ex}");
				}
			}

			Plugins = list;
		}

		static DalamudHelper()
		{
			Logger = new("DalamudHelper");
			Plugins = new List<PluginEntry>();
		}
	}

	public static class ReflectionExtensions
	{
		/// <summary>
		///     Returns a _private_ Property Value from a given Object. Uses Reflection.
		///     Throws a ArgumentOutOfRangeException if the Property is not found.
		/// </summary>
		/// <typeparam name="T">Type of the Property</typeparam>
		/// <param name="obj">Object from where the Property Value is returned</param>
		/// <param name="propName">PropertyName as string.</param>
		/// <returns>PropertyValue</returns>
		public static T GetPropertyValue<T>(this object obj, string propName)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			PropertyInfo? pi = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (pi == null)
			{
				throw new ArgumentOutOfRangeException(nameof(propName), string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
			}

			return (T) pi.GetValue(obj, null)!;
		}

		/// <summary>
		///     Set a _private_ Property Value in a given Object. Uses Reflection.
		///     Throws a ArgumentOutOfRangeException if the Property is not found.
		/// </summary>
		/// <typeparam name="T">Type of the Property</typeparam>
		/// <param name="obj">Object from where the Property Value is modified</param>
		/// <param name="propName">PropertyName as string.</param>
		/// <param name="value">New value.</param>
		public static void SetPropertyValue<T>(this object obj, string propName, T value)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			PropertyInfo? pi = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (pi == null)
			{
				throw new ArgumentOutOfRangeException(nameof(propName), string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
			}

			pi.SetValue(obj!, value);
		}

		/// <summary>
		///     Returns a private Property Value from a given Object. Uses Reflection.
		///     Throws a ArgumentOutOfRangeException if the Property is not found.
		/// </summary>
		/// <typeparam name="T">Type of the Property</typeparam>
		/// <param name="obj">Object from where the Property Value is returned</param>
		/// <param name="propName">PropertyName as string.</param>
		/// <returns>PropertyValue</returns>
		public static T GetFieldValue<T>(this object obj, string propName)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			Type? t = obj.GetType();
			FieldInfo? fi = null;
			while (fi == null && t != null)
			{
				fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				t = t.BaseType;
			}

			if (fi == null)
			{
				throw new ArgumentOutOfRangeException(nameof(propName), string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
			}

			return (T) fi.GetValue(obj)!;
		}

		/// <summary>
		///     Sets a private Property Value in a given Object. Uses Reflection.
		///     Throws a ArgumentOutOfRangeException if the Property is not found.
		/// </summary>
		/// <typeparam name="T">Type of the Property</typeparam>
		/// <param name="obj">Object from where the Property Value is modified</param>
		/// <param name="propName">PropertyName as string.</param>
		/// <param name="value">New value.</param>
		public static void SetFieldValue<T>(this object obj, string propName, T? value)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			Type? t = obj.GetType();
			FieldInfo? fi = null;
			while (fi == null && t != null)
			{
				fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				t = t.BaseType;
			}

			if (fi == null)
			{
				throw new ArgumentOutOfRangeException(nameof(propName), string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
			}

			fi.SetValue(obj!, value);
		}
	}
}