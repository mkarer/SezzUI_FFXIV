using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Newtonsoft.Json;
using SezzUI.Configuration.Attributes;
using SezzUI.Configuration.Profiles;
using SezzUI.Helper;
using SezzUI.Logging;

namespace SezzUI.Configuration.Tree;

public class ConfigPageNode : SubSectionNode
{
	private PluginConfigObject _configObject = null!;
	private List<ConfigNode>? _drawList;
	private Dictionary<string, ConfigPageNode> _nestedConfigPageNodes = null!;
	internal PluginLogger Logger;

	public ConfigPageNode()
	{
		Logger = new(GetType().Name);
	}

	public PluginConfigObject ConfigObject
	{
		get => _configObject;
		set
		{
			_configObject = value;
			GenerateNestedConfigPageNodes();
			_drawList = null;
		}
	}

	private void GenerateNestedConfigPageNodes()
	{
		_nestedConfigPageNodes = new();

		FieldInfo[] fields = _configObject.GetType().GetFields();

		foreach (FieldInfo field in fields)
		{
			foreach (object attribute in field.GetCustomAttributes(true))
			{
				if (attribute is not NestedConfigAttribute nestedConfigAttribute)
				{
					continue;
				}

				object? value = field.GetValue(_configObject);

				if (value is not PluginConfigObject nestedConfig)
				{
					continue;
				}

				ConfigPageNode configPageNode = new();
				configPageNode.ConfigObject = nestedConfig;
				configPageNode.Name = nestedConfigAttribute.friendlyName;

				if (nestedConfig.Disableable)
				{
					configPageNode.Name += "##" + nestedConfig.GetHashCode();
				}

				_nestedConfigPageNodes.Add(field.Name, configPageNode);
			}
		}
	}

	public override string? GetBase64String()
	{
		if (!AllowShare())
		{
			return null;
		}

		return ImportExportHelper.GenerateExportString(ConfigObject);
	}

	protected override bool AllowExport() => ConfigObject.Exportable;

	protected override bool AllowShare() => ConfigObject.Shareable;

	protected override bool AllowReset() => ConfigObject.Resettable;

	public override bool Draw(ref bool changed) => DrawWithID(ref changed);

	private bool DrawWithID(ref bool changed, string? ID = null)
	{
		bool didReset = false;

		// Only do this stuff the first time the config page is loaded
		if (_drawList is null)
		{
			_drawList = GenerateDrawList();
		}

		if (_drawList is not null)
		{
			foreach (ConfigNode fieldNode in _drawList)
			{
				didReset |= fieldNode.Draw(ref changed);
			}
		}

		didReset |= DrawPortableSection();

		ImGui.NewLine(); // fixes some long pages getting cut off

		return didReset;
	}

	private List<ConfigNode> GenerateDrawList(string? ID = null)
	{
		Dictionary<string, ConfigNode> fieldMap = new();

		FieldInfo[] fields = ConfigObject.GetType().GetFields();
		foreach (FieldInfo field in fields)
		{
			if (ConfigObject.DisableParentSettings != null && ConfigObject.DisableParentSettings.Contains(field.Name))
			{
				continue;
			}

			foreach (object attribute in field.GetCustomAttributes(true))
			{
				if (attribute is NestedConfigAttribute nestedConfigAttribute && _nestedConfigPageNodes.TryGetValue(field.Name, out ConfigPageNode? node))
				{
					List<ConfigNode> newNodes = node.GenerateDrawList(node.Name);
					foreach (ConfigNode newNode in newNodes)
					{
						newNode.Position = nestedConfigAttribute.pos;
						newNode.Separator = nestedConfigAttribute.separator;
						newNode.Spacing = nestedConfigAttribute.spacing;
						newNode.ParentName = nestedConfigAttribute.collapseWith;
						newNode.Nest = nestedConfigAttribute.nest;
						newNode.CollapsingHeader = nestedConfigAttribute.collapsingHeader;
						fieldMap.Add($"{node.Name}_{newNode.Name}", newNode);
					}
				}
				else if (attribute is OrderAttribute orderAttribute)
				{
					FieldNode fieldNode = new(field, ConfigObject, ID);
					fieldNode.Position = orderAttribute.pos;
					fieldNode.ParentName = orderAttribute.collapseWith;
					fieldMap.Add(field.Name, fieldNode);
				}
			}
		}

		IEnumerable<MethodInfo> manualDrawMethods = ConfigObject.GetType().GetMethods().Where(m => Attribute.IsDefined(m, typeof(ManualDrawAttribute), false));
		foreach (MethodInfo method in manualDrawMethods)
		{
			string id = $"ManualDraw##{method.GetHashCode()}";
			fieldMap.Add(id, new ManualDrawNode(method, ConfigObject, id));
		}

		foreach (ConfigNode configNode in fieldMap.Values)
		{
			if (configNode.ParentName is not null && fieldMap.TryGetValue(configNode.ParentName, out ConfigNode? parentNode))
			{
				if (!ConfigObject.Disableable && parentNode.Name.Equals("Enabled") && parentNode.ID is null)
				{
					continue;
				}

				if (parentNode is FieldNode parentFieldNode)
				{
					parentFieldNode.CollapseControl = true;
					parentFieldNode.AddChild(configNode.Position, configNode);
				}
			}
		}

		List<ConfigNode> fieldNodes = fieldMap.Values.ToList();
		fieldNodes.RemoveAll(f => f.IsChild);
		fieldNodes.Sort((x, y) => x.Position - y.Position);
		return fieldNodes;
	}

	private bool DrawPortableSection()
	{
		if (!AllowExport())
		{
			return false;
		}

		ImGuiHelper.DrawSeparator(2, 1);

		const float buttonWidth = 120;

		ImGui.BeginGroup();

		ImGui.SetCursorPos(new(ImGui.GetWindowContentRegionMax().X / 2f - buttonWidth - 5, ImGui.GetCursorPosY()));

		if (ImGui.Button("Export", new(buttonWidth, 24)))
		{
			string exportString = ImportExportHelper.GenerateExportString(ConfigObject);
			ImGui.SetClipboardText(exportString);
		}

		ImGui.SameLine();

		if (ImGui.Button("Reset", new(buttonWidth, 24)))
		{
			_nodeToReset = this;
			_nodeToResetName = Utils.UserFriendlyConfigName(ConfigObject.GetType().Name);
		}

		ImGui.NewLine();
		ImGui.EndGroup();

		return DrawResetModal();
	}

	private static string ReplaceLastOccurrence(string Source, string Find, string Replace)
	{
		int place = Source.LastIndexOf(Find);
		if (place == -1)
		{
			return Source;
		}

		string result = Source.Remove(place, Find.Length).Insert(place, Replace);
		return result;
	}

	public override void Save(string path)
	{
		string[] splits = path.Split("\\", StringSplitOptions.RemoveEmptyEntries);
		string directory = ReplaceLastOccurrence(path, splits.Last(), "");
		Directory.CreateDirectory(directory);

		string finalPath = path + ".json";

		try
		{
			File.WriteAllText(finalPath, JsonConvert.SerializeObject(ConfigObject, Formatting.Indented, new JsonSerializerSettings {TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple, TypeNameHandling = TypeNameHandling.Objects}));
		}
		catch (Exception ex)
		{
			Logger.Error($"Error while saving config object: {ex}");
		}
	}

	public override void Load(string path, string currentVersion, string? previousVersion = null)
	{
		if (ConfigObject is not PluginConfigObject)
		{
			return;
		}

		FileInfo finalPath = new(path + ".json");

		// Use reflection to call the LoadForType method, this allows us to specify a type at runtime.
		// While in general use this is important as the conversion from the superclass 'PluginConfigObject' to a specific subclass (e.g. 'BlackMageHudConfig') would
		// be handled by Json.NET, when the plugin is reloaded with a different assembly (as is the case when using LivePluginLoader, or updating the plugin in-game)
		// it fails. In order to fix this we need to specify the specific subclass, in order to do this during runtime we must use reflection to set the generic.
		MethodInfo? methodInfo = ConfigObject.GetType().GetMethod("Load");
		MethodInfo? function = methodInfo?.MakeGenericMethod(ConfigObject.GetType());

		object[] args = previousVersion != null ? new object[] {finalPath, currentVersion, previousVersion} : new object[] {finalPath, currentVersion};
		PluginConfigObject? config = (PluginConfigObject?) function?.Invoke(ConfigObject, args);

		ConfigObject = config ?? ConfigObject;
	}

	public override void Reset()
	{
		bool hasReset = false;

		try
		{
			// TODO: Every PluginConfigObject should implement Reset()
			MethodInfo? resetMethod = ConfigObject.GetType().GetMethod("Reset", BindingFlags.Public | BindingFlags.Instance);
			if (resetMethod != null)
			{
				Logger.Debug($"Resetting {ConfigObject.GetType()} to defaults...");
				resetMethod!.Invoke(ConfigObject, null);
				hasReset = true;
			}
			else
			{
				Logger.Warning($"{ConfigObject.GetType()} should implement Reset(), will create a new instance instead!");
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Error resetting {ConfigObject.GetType()}: {ex}");
		}

		if (!hasReset)
		{
			ConfigObject = ConfigurationManager.GetDefaultConfigObjectForType(ConfigObject.GetType());
		}

		Singletons.Get<ConfigurationManager>().OnConfigObjectReset(ConfigObject);
	}

	public override ConfigPageNode? GetOrAddConfig<T>() => this;
}