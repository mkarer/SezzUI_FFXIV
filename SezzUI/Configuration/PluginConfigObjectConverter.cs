using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SezzUI.Interface.GeneralElements;
using SezzUI.Logging;

namespace SezzUI.Configuration
{
	public abstract class PluginConfigObjectConverter : JsonConverter
	{
		protected Dictionary<string, PluginConfigObjectFieldConverter> FieldConvertersMap = new();
		internal PluginLogger Logger;

		protected PluginConfigObjectConverter()
		{
			Logger = new(GetType().Name);
		}

		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			MethodInfo? genericMethod = GetType().GetMethod("ConvertJson");
			MethodInfo? method = genericMethod?.MakeGenericMethod(objectType);
			return method?.Invoke(this, new object[] {reader, serializer});
		}

		public T? ConvertJson<T>(JsonReader reader, JsonSerializer serializer) where T : PluginConfigObject
		{
			Type type = typeof(T);
			T? config = null;

			try
			{
				ConstructorInfo? constructor = type.GetConstructor(new Type[] { });
				if (constructor != null)
				{
					config = (T?) Activator.CreateInstance<T>();
				}
				else
				{
					config = (T?) ConfigurationManager.GetDefaultConfigObjectForType(type);
				}

				// last resource, hackily create an instance without calling the constructor
				if (config == null)
				{
					config = (T) RuntimeHelpers.GetUninitializedObject(type);
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Error creating a {type.Name}: {ex}");
			}

			if (config == null)
			{
				return null;
			}

			try
			{
				JObject? jsonObject = (JObject?) serializer.Deserialize(reader);
				if (jsonObject == null)
				{
					return null;
				}

				Dictionary<string, object> ValuesMap = new();

				// get values from json
				foreach (JProperty property in jsonObject.Properties())
				{
					string propertyName = property.Name;
					object? value = null;

					// convert values if needed
					if (FieldConvertersMap.TryGetValue(propertyName, out PluginConfigObjectFieldConverter? fieldConverter) && fieldConverter != null)
					{
						(propertyName, value) = fieldConverter.Convert(property.Value);
					}
					// read value from json
					else
					{
						FieldInfo? field = type.GetField(propertyName);
						if (field != null)
						{
							value = property.Value.ToObject(field.FieldType);
						}
					}

					if (value != null)
					{
						ValuesMap.Add(propertyName, value);
					}
				}

				// apply values
				foreach (string key in ValuesMap.Keys)
				{
					string[] fields = key.Split(".", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					object? currentObject = config;
					object value = ValuesMap[key];

					for (int i = 0; i < fields.Length; i++)
					{
						FieldInfo? field = currentObject?.GetType().GetField(fields[i]);
						if (field == null)
						{
							break;
						}

						if (i == fields.Length - 1 && value.GetType() == field.FieldType)
						{
							field.SetValue(currentObject, value);
						}
						else
						{
							currentObject = field.GetValue(currentObject);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Error deserializing {type.Name}: {ex}");
			}

			return config;
		}


		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			if (value == null)
			{
				return;
			}

			JObject jsonObject = new();
			Type type = value.GetType();
			jsonObject.Add("$type", type.FullName + ", SezzUI");

			FieldInfo[] fields = type.GetFields();

			foreach (FieldInfo field in fields)
			{
				if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
				{
					continue;
				}

				object? fieldValue = field.GetValue(value);
				if (fieldValue != null)
				{
					jsonObject.Add(field.Name, JToken.FromObject(fieldValue, serializer));
				}
			}

			jsonObject.WriteTo(writer);
		}
	}

	#region contract resolver

	public class PluginConfigObjectsContractResolver : DefaultContractResolver
	{
		private static readonly Dictionary<Type, Type> ConvertersMap = new()
		{
			[typeof(HUDOptionsConfig)] = typeof(HUDOptionsConfigConverter)
		};

		protected override JsonObjectContract CreateObjectContract(Type objectType)
		{
			JsonObjectContract contract = base.CreateObjectContract(objectType);

			if (ConvertersMap.TryGetValue(objectType, out Type? converterType) && converterType != null)
			{
				contract.Converter = (JsonConverter?) Activator.CreateInstance(converterType);
			}

			return contract;
		}
	}

	#endregion

	#region field converters

	public abstract class PluginConfigObjectFieldConverter
	{
		public readonly string NewFieldPath;

		public PluginConfigObjectFieldConverter(string newFieldPath)
		{
			NewFieldPath = newFieldPath;
		}

		public abstract (string, object) Convert(JToken token);
	}

	public class NewTypeFieldConverter<TOld, TNew> : PluginConfigObjectFieldConverter where TOld : struct where TNew : struct
	{
		private readonly TNew DefaultValue;
		private readonly Func<TOld, TNew> Func;

		public NewTypeFieldConverter(string newFieldPath, TNew defaultValue, Func<TOld, TNew> func) : base(newFieldPath)
		{
			DefaultValue = defaultValue;
			Func = func;
		}

		public override (string, object) Convert(JToken token)
		{
			TNew result = DefaultValue;

			TOld? oldValue = token.ToObject<TOld>();
			if (oldValue.HasValue)
			{
				result = Func(oldValue.Value);
			}

			return (NewFieldPath, result);
		}
	}

	public class SameTypeFieldConverter<T> : NewTypeFieldConverter<T, T> where T : struct
	{
		public SameTypeFieldConverter(string newFieldPath, T defaultValue) : base(newFieldPath, defaultValue, oldValue => { return oldValue; })
		{
		}
	}

	public class NewClassFieldConverter<TOld, TNew> : PluginConfigObjectFieldConverter where TOld : class where TNew : class
	{
		private readonly TNew DefaultValue;
		private readonly Func<TOld, TNew> Func;

		public NewClassFieldConverter(string newFieldPath, TNew defaultValue, Func<TOld, TNew> func) : base(newFieldPath)
		{
			DefaultValue = defaultValue;
			Func = func;
		}

		public override (string, object) Convert(JToken token)
		{
			TNew result = DefaultValue;

			TOld? oldValue = token.ToObject<TOld>();
			if (oldValue != null)
			{
				result = Func(oldValue);
			}

			return (NewFieldPath, result);
		}
	}

	public class SameClassFieldConverter<T> : NewClassFieldConverter<T, T> where T : class
	{
		public SameClassFieldConverter(string newFieldPath, T defaultValue) : base(newFieldPath, defaultValue, oldValue => { return oldValue; })
		{
		}
	}

	public class TypeToClassFieldConverter<TOld, TNew> : PluginConfigObjectFieldConverter where TOld : struct where TNew : class
	{
		private readonly TNew DefaultValue;
		private readonly Func<TOld, TNew> Func;

		public TypeToClassFieldConverter(string newFieldPath, TNew defaultValue, Func<TOld, TNew> func) : base(newFieldPath)
		{
			DefaultValue = defaultValue;
			Func = func;
		}

		public override (string, object) Convert(JToken token)
		{
			TNew result = DefaultValue;

			TOld? oldValue = token.ToObject<TOld>();
			if (oldValue.HasValue)
			{
				result = Func(oldValue.Value);
			}

			return (NewFieldPath, result);
		}
	}

	#endregion
}