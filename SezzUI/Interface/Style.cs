using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using SezzUI.Configuration;

namespace SezzUI.Interface
{
	public static class Style
	{
		public static class Colors
		{
			public static PluginConfigColor Information = new(new(150f / 255f, 209f / 255f, 1f, 1f)); // Blue
			public static PluginConfigColor Alert = new(new(255f / 255f, 234f / 255f, 119f / 255f, 1f)); // Yellow
			public static PluginConfigColor Error = new(new(221f / 255f, 25f / 255f, 0f / 255f, 1f)); // Red
			public static PluginConfigColor Hint = new(new(0.6f, 0.6f, 0.6f, 1f)); // Gray

			// Generic
			public static PluginConfigColor Black = new(new(0f, 0f, 0f, 1f));
			public static PluginConfigColor White = new(new(1f, 1f, 1f, 1f));
		}

		private static readonly Dictionary<Set, Dictionary<ImGuiCol, Vector4>> _styleColors = new()
		{
			{
				Set.Button, new()
				{
					{ImGuiCol.Button, new(46f / 255f, 45f / 255f, 46f / 255f, 1f)},
					{ImGuiCol.ButtonHovered, new(0f / 255f, 174f / 255f, 255f / 255f, 1f)},
					{ImGuiCol.ButtonActive, new(0f / 255f, 139f / 255f, 204f / 255f, 1f)}
				}
			},
			{
				Set.ButtonDangerous, new()
				{
					{ImGuiCol.Button, new(102f / 255f, 41f / 255f, 41f / 255f, 1f)},
					{ImGuiCol.ButtonHovered, new(148f / 255f, 41f / 255f, 41f / 255f, 1f)},
					{ImGuiCol.ButtonActive, new(182f / 255f, 41f / 255f, 41f / 255f, 1f)}
				}
			}
		};

		private static readonly Dictionary<Set, Dictionary<ImGuiStyleVar, dynamic>> _styleVars = new();

		private static Set? _activeSet;

		public static void Push(Set set)
		{
			_activeSet = set;

			if (_styleColors.ContainsKey(set))
			{
				foreach ((ImGuiCol color, Vector4 value) in _styleColors[set])
				{
					ImGui.PushStyleColor(color, value);
				}
			}

			if (_styleVars.ContainsKey(set))
			{
				foreach ((ImGuiStyleVar var, dynamic value) in _styleVars[set])
				{
					ImGui.PushStyleVar(var, value);
				}
			}
		}

		public static void Pop(Set set)
		{
			if (_styleColors.ContainsKey(set))
			{
				ImGui.PopStyleColor(_styleColors[set].Count);
			}

			if (_styleVars.ContainsKey(set))
			{
				ImGui.PopStyleVar(_styleVars[set].Count);
			}
		}

		public static void Pop()
		{
			if (_activeSet != null)
			{
				Pop((Set) _activeSet);
				_activeSet = null;
			}
		}
	}

	public enum Set
	{
		Button,
		ButtonDangerous
	}
}