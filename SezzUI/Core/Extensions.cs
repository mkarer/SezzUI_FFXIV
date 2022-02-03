using System;
using System.Numerics;
using System.Reflection;
using SezzUI.Enums;
using static System.Globalization.CultureInfo;

namespace SezzUI
{
	public static class Extensions
	{
		public static string Truncate(this string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
			{
				return str;
			}

			return str.Length <= maxLength ? str : str[..maxLength];
		}

		public static Vector4 AdjustColor(this Vector4 vec, float correctionFactor)
		{
			float red = vec.X;
			float green = vec.Y;
			float blue = vec.Z;

			if (correctionFactor < 0)
			{
				correctionFactor = 1 + correctionFactor;
				red *= correctionFactor;
				green *= correctionFactor;
				blue *= correctionFactor;
			}
			else
			{
				red = (1 - red) * correctionFactor + red;
				green = (1 - green) * correctionFactor + green;
				blue = (1 - blue) * correctionFactor + blue;
			}

			return new(red, green, blue, vec.W);
		}

		public static Vector2 AddX(this Vector2 v, float offset) => new(v.X + offset, v.Y);

		public static Vector2 AddY(this Vector2 v, float offset) => new(v.X, v.Y + offset);

		public static Vector2 AddXY(this Vector2 v, float offset) => new(v.X + offset, v.Y + offset);

		public static Vector2 AddXY(this Vector2 v, float xOffset, float yOffset) => new(v.X + xOffset, v.Y + yOffset);

		public static Vector4 AddTransparency(this Vector4 vec, float opacity) => new(vec.X, vec.Y, vec.Z, vec.W * opacity);

		public static Vector4 WithNewAlpha(this Vector4 vec, float alpha) => new(vec.X, vec.Y, vec.Z, alpha);

		public static string KiloFormat(this uint num)
		{
			return num switch
			{
				>= 100000000 => (num / 1000000.0).ToString("#,0M", InvariantCulture),
				>= 1000000 => (num / 1000000.0).ToString("0.0", InvariantCulture) + "M",
				>= 100000 => (num / 1000.0).ToString("#,0K", InvariantCulture),
				>= 10000 => (num / 1000.0).ToString("0.0", InvariantCulture) + "K",
				_ => num.ToString("#,0", InvariantCulture)
			};
		}

		public static bool IsHorizontal(this BarDirection direction) => direction == BarDirection.Right || direction == BarDirection.Left;

		public static bool IsInverted(this BarDirection direction) => direction == BarDirection.Left || direction == BarDirection.Up;
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

			pi.SetValue(obj, value);
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

			fi.SetValue(obj, value);
		}
	}
}