using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Utility;
using Newtonsoft.Json;
using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;
using SezzUI.Enums;
using SezzUI.Helper;
using SezzUI.Interface;
using SezzUI.Interface.BarManager;

namespace SezzUI.Modules.CooldownHud;

#region General

[Section("Cooldown HUD")]
[SubSection("General", 0)]
public class CooldownHudConfig : PluginConfigObject
{
	[NestedConfig("Pulse Animation", 20)]
	public CooldownHudPulseConfig CooldownHudPulse = new();

	[NestedConfig("Default Bar Style", 30, collapsingHeader = true)]
	public CooldownHudBarManagerBarConfig DefaultStyle = new();

	[JsonIgnore]
	private CooldownHudGroupsConfig? _groupsConfig;

	[JsonIgnore]
	public CooldownHudGroupsConfig? Groups
	{
		get
		{
			if (_groupsConfig == null && Singletons.IsRegistered<ConfigurationManager>())
			{
				_groupsConfig ??= Singletons.Get<ConfigurationManager>().GetConfigObject<CooldownHudGroupsConfig>();
			}

			return _groupsConfig;
		}
	}

	public void Reset()
	{
		Enabled = true;
		CooldownHudPulse.Reset();
		DefaultStyle.Reset();
		Groups?.Reset();
	}

	public CooldownHudConfig()
	{
		Reset();
	}

	public new static CooldownHudConfig DefaultConfig() => new();
}

#endregion

#region Pulse

public class CooldownHudPulseConfig : AnchorablePluginConfigObject
{
	[DragInt("Animation Delay [ms]", min = -5000, max = 0)]
	[Order(30, collapseWith = nameof(Enabled))]
	public int Delay = -400;

	public void Reset()
	{
		Enabled = true;
		Delay = -400;
		Position = new(430f, -208f);
		Size = new(32f, 32f);
		Anchor = DrawAnchor.Center;
	}

	public CooldownHudPulseConfig()
	{
		Reset();
	}

	public new static CooldownHudPulseConfig DefaultConfig() => new();
}

#endregion

#region Groups/BarManagers

[Disableable(false)]
[Resettable(false)]
[Section("Cooldown HUD")]
[SubSection("Cooldown Groups", 0)]
public class CooldownHudGroupsConfig : PluginConfigObject
{
	[JsonIgnore]
	public List<CooldownHudGroupDetailConfig> Groups;

	[JsonIgnore]
	private CooldownHudConfig? _cooldownHudConfig;

	[JsonIgnore]
	public CooldownHudConfig? CooldownHud
	{
		get
		{
			if (_cooldownHudConfig == null && Singletons.IsRegistered<ConfigurationManager>())
			{
				_cooldownHudConfig ??= Singletons.Get<ConfigurationManager>().GetConfigObject<CooldownHudConfig>();
			}

			return _cooldownHudConfig;
		}
	}

	[JsonIgnore]
	private int? _groupRemovalRequested;

	[JsonIgnore]
	private bool _groupRemovalConfirmed;

	[ManualDraw]
	public bool Draw(ref bool changed)
	{
		bool cooldownHudEnabled = CooldownHud?.Enabled ?? false;
		if (!cooldownHudEnabled)
		{
			ImGuiHelper.DrawErrorNotice("Cooldown HUD is disabled.", 0);
			ImGui.NewLine();
		}

		ImGuiHelper.DrawInformationNotice("Groups are containers used to display cooldown bars.\nYou can use as many of them you want to group up similar actions on different areas of the screen but you need at least one group to track anything!", 1);
		ImGui.NewLine();

		if (ImGui.BeginTabBar("##SezzUI_CooldownHudGroupsConfig", ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.NoCloseWithMiddleMouseButton | ImGuiTabBarFlags.FittingPolicyScroll))
		{
			if (ImGui.TabItemButton("+", ImGuiTabItemFlags.Leading))
			{
				Groups.Add(new());
			}

			for (int i = 0; i < Groups.Count(); i++)
			{
				CooldownHudGroupDetailConfig group = Groups[i];
				string groupTitle = $"{(group.Description.IsNullOrWhitespace() ? "Untitled Group" : group.Description)}##SezzUI_CooldownHudGroupsConfig{i}";

				if (ImGui.BeginTabItem(groupTitle))
				{
					// Removal button
					float buttonWidth = 120;
					ImGui.BeginGroup();
					ImGui.NewLine();
					ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X / 2f - buttonWidth / 2f);
					Style.Push(Set.ButtonDangerous);
					if (ImGuiHelper.FontAwesomeIconButton("Remove Group", FontAwesomeIcon.Times, new(buttonWidth, 0)))
					{
						_groupRemovalRequested = i;
						_groupRemovalConfirmed = ImGui.GetIO().KeyAlt;
					}

					Style.Pop();
					ImGui.EndGroup();

					// End of group
					ImGui.EndTabItem();
				}
			}

			ImGui.EndTabBar();

			if (!Groups.Any())
			{
				ImGuiHelper.DrawAlertNotice(cooldownHudEnabled ? "No groups configured, please add one first or disable the module if you don't intend to use it." : "No groups configured.", 2);
				ImGui.NewLine();
			}

			// Removal
			if (_groupRemovalRequested != null)
			{
				(bool, bool) dialogResult = (_groupRemovalConfirmed, false);
				if (!dialogResult.Item1)
				{
					dialogResult = ImGuiHelper.DrawConfirmationModal("Remove Cooldown Group", new[] {"Do you really want to remove that group?", "\u24d8Hint: Hold ALT while pressing the remove button to suppress the confirmation dialog."});
				}

				if (dialogResult.Item1 && Groups.Count() > _groupRemovalRequested)
				{
					Groups.RemoveAt((int) _groupRemovalRequested);
				}

				if (dialogResult.Item1 || dialogResult.Item2)
				{
					_groupRemovalRequested = null;
					_groupRemovalConfirmed = false;
				}
			}
		}

		return changed;
	}

	public void Reset()
	{
		Groups.ForEach(group => group.Reset());
	}

	public CooldownHudGroupsConfig()
	{
		Groups = new();
		Reset();
	}

	public new static CooldownHudGroupsConfig DefaultConfig() => new();
}

#endregion

#region Single Group/BarManager

[Disableable(false)]
[DisableParentSettings("Enabled", "Size")]
public class CooldownHudGroupDetailConfig : AnchorablePluginConfigObject
{
	[InputText("Description (Optional)", formattable = false)]
	[Order(0)]
	public string Description = "";

	[NestedConfig("Bar Style", 20)]
	[Order(10)]
	public CooldownHudBarManagerBarConfig BarConfig = new();

	[JsonIgnore]
	public List<uint> Actions;

	public void Reset()
	{
		Enabled = false;
		Description = "";
		Actions.Clear();
		BarConfig.Reset();
	}

	public CooldownHudGroupDetailConfig()
	{
		Actions = new();
		Reset();
	}

	public new static CooldownHudGroupDetailConfig DefaultConfig() => new();
}

/// <summary>
///     Custom defaults for cooldown bars.
/// </summary>
[Disableable(false)]
public class CooldownHudBarManagerBarConfig : BarManagerBarConfig
{
	public override void Reset()
	{
		base.Reset();

		Style = BarManagerStyle.Ruri;
		SetDefaultStyleAttributes();

		FillInverted = true;
		ShowDurationRemaining = true;
	}
}

#endregion