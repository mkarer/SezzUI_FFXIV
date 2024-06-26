﻿using System.Collections.Generic;
using Newtonsoft.Json;
using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Modules.GameUI;

[Section("Game UI")]
[SubSection("Element Hiding", 0)]
public class ElementHiderConfig : PluginConfigObject
{
	[Checkbox("Hide ActionBar Lock", isMonitored = true)]
	[Order(1)]
	public bool HideActionBarLock;

	[Checkbox("Restore Visibility On Logout/Disable", help = "Sets visibility state of all selected elements to \"visible\" when you disable the module or logout.")]
	[Order(5)]
	public bool RestoreVisibility;

	[NestedConfig("Area 1", 21)]
	public InteractableAreaConfig Area1 = new();

	[NestedConfig("Area 2", 22)]
	public InteractableAreaConfig Area2 = new();

	[NestedConfig("Area 3", 23)]
	public InteractableAreaConfig Area3 = new();

	[NestedConfig("Area 4", 24)]
	public InteractableAreaConfig Area4 = new();

	[NestedConfig("Area 5", 25)]
	public InteractableAreaConfig Area5 = new();

	[NestedConfig("Area 6", 26)]
	public InteractableAreaConfig Area6 = new();

	[NestedConfig("Area 7", 27)]
	public InteractableAreaConfig Area7 = new();

	[NestedConfig("Area 8", 28)]
	public InteractableAreaConfig Area8 = new();

	[NestedConfig("Area 9", 29)]
	public InteractableAreaConfig Area9 = new();

	[NestedConfig("Area 10", 30)]
	public InteractableAreaConfig Area10 = new();

	[JsonIgnore]
	public List<InteractableAreaConfig> Areas;

	public void Reset()
	{
		Enabled = true;
		HideActionBarLock = true;
		RestoreVisibility = false;
		Areas.ForEach(area => area.Reset());
	}

	public ElementHiderConfig()
	{
		Areas = new() {Area1, Area2, Area3, Area4, Area5, Area6, Area7, Area8, Area9, Area10};
		Reset();
	}

	public new static ElementHiderConfig DefaultConfig() => new();
}