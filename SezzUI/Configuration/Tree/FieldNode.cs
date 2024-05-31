using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using SezzUI.Configuration.Attributes;
using SezzUI.Helper;

namespace SezzUI.Configuration.Tree;

public abstract class ConfigNode
{
	public bool CollapseControl { get; set; }

	public bool IsChild { get; set; }

	public string Name { get; }

	public bool Nest { get; set; } = true;

	public string? ParentName { get; set; }

	public int Position { get; set; } = int.MaxValue;

	public bool Separator { get; set; }

	public bool Spacing { get; set; }

	public bool CollapsingHeader { get; set; }

	public string? ID { get; }

	protected PluginConfigObject ConfigObject { get; set; }

	public ConfigNode(PluginConfigObject configObject, string? id, string name)
	{
		ConfigObject = configObject;
		ID = id;
		Name = name;
	}

	public abstract bool Draw(ref bool changed, int depth = 0);

	protected void DrawSeparatorOrSpacing()
	{
		if (Separator)
		{
			ImGuiHelper.DrawSeparator(1, 1);
		}

		if (Spacing)
		{
			ImGuiHelper.DrawSpacing(1);
		}
	}

	protected static ConfigAttribute? GetConfigAttribute(FieldInfo field)
	{
		return field.GetCustomAttributes(true).Where(a => a is ConfigAttribute).FirstOrDefault() as ConfigAttribute;
	}
}

public class FieldNode : ConfigNode
{
	private readonly SortedDictionary<int, ConfigNode> _childNodes;
	private readonly ConfigAttribute? _configAttribute;
	private readonly FieldInfo _mainField;

	public FieldNode(FieldInfo mainField, PluginConfigObject configObject, string? id) : base(configObject, id, mainField.Name)
	{
		_mainField = mainField;
		_childNodes = new();

		_configAttribute = GetConfigAttribute(mainField);
		if (_configAttribute is not null)
		{
			Separator = _configAttribute.separator;
			Spacing = _configAttribute.spacing;
		}
	}

	public void AddChild(int position, ConfigNode field)
	{
		field.IsChild = true;

		while (_childNodes.ContainsKey(position))
		{
			position++;
		}

		_childNodes.Add(position, field);
	}

	public override bool Draw(ref bool changed, int depth = 0)
	{
		bool reset = false;
		DrawSeparatorOrSpacing();

		if (!Nest)
		{
			depth = 0;
		}

		if (depth > 0)
		{
			ImGuiHelper.DrawNestIndicator(depth);
		}

		bool collapsing = CollapsingHeader && ConfigObject.Disableable;

		// Draw the ConfigAttribute
		if (!collapsing)
		{
			DrawConfigAttribute(ref changed, _mainField);
		}

		bool enabled = _mainField.GetValue(ConfigObject) as bool? ?? false;

		// Draw children
		if (CollapseControl && Attribute.IsDefined(_mainField, typeof(CheckboxAttribute)))
		{
			if (collapsing)
			{
				if (ImGui.CollapsingHeader(ID + "##CollapsingHeader"))
				{
					ImGuiHelper.DrawNestIndicator(depth);
					DrawConfigAttribute(ref changed, _mainField);

					if (enabled)
					{
						reset |= DrawChildren(ref changed, depth);
					}
				}
			}
			else if (!collapsing && enabled)
			{
				ImGui.BeginGroup();
				reset |= DrawChildren(ref changed, depth);
				ImGui.EndGroup();
			}
		}

		return reset;
	}

	private bool DrawChildren(ref bool changed, int depth)
	{
		bool reset = false;

		int childDepth = depth + 1;
		foreach (ConfigNode child in _childNodes.Values)
		{
			if (child.Separator)
			{
				childDepth = 0;
			}

			reset |= child.Draw(ref changed, childDepth);
		}

		return reset;
	}

	private void DrawConfigAttribute(ref bool changed, FieldInfo field)
	{
		if (_configAttribute is not null)
		{
			changed |= _configAttribute.Draw(field, ConfigObject, ID, CollapsingHeader);
		}
	}
}

public class ManualDrawNode : ConfigNode
{
	private readonly MethodInfo _drawMethod;

	public ManualDrawNode(MethodInfo method, PluginConfigObject configObject, string? id) : base(configObject, id, id ?? "")
	{
		_drawMethod = method;
	}

	public override bool Draw(ref bool changed, int depth = 0)
	{
		object[] args = {false};
		bool? result = (bool?) _drawMethod.Invoke(ConfigObject, args);

		bool arg = (bool) args[0];
		changed |= arg;
		return result ?? false;
	}
}