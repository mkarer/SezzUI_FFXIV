﻿using System.Collections.Generic;
using System.Numerics;
using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;
using SezzUI.Enums;

namespace SezzUI.Modules.GameUI;

public class InteractableAreaConfig : AnchorablePluginConfigObject
{
	[InputText("Description (Optional)", formattable = false)]
	[Order(0)]
	public string Description = "";

	[MultiSelector("Elements", isMonitored = true)]
	[IntStringPair((int) Addon.ActionBar1, "Action Bar 1")]
	[IntStringPair((int) Addon.ActionBar2, "Action Bar 2")]
	[IntStringPair((int) Addon.ActionBar3, "Action Bar 3")]
	[IntStringPair((int) Addon.ActionBar4, "Action Bar 4")]
	[IntStringPair((int) Addon.ActionBar5, "Action Bar 5")]
	[IntStringPair((int) Addon.ActionBar6, "Action Bar 6")]
	[IntStringPair((int) Addon.ActionBar7, "Action Bar 7")]
	[IntStringPair((int) Addon.ActionBar8, "Action Bar 8")]
	[IntStringPair((int) Addon.ActionBar9, "Action Bar 9")]
	[IntStringPair((int) Addon.ActionBar10, "Action Bar 10")]
	[IntStringPair((int) Addon.PetActionBar, "Pet Action Bar")]
	[IntStringPair((int) Addon.MainMenu, "Main Menu")]
	[IntStringPair((int) Addon.ScenarioGuide, "Scenario Guide")]
	[IntStringPair((int) Addon.QuestLog, "Duty List")]
	[Order(20)]
	// ReSharper disable once CollectionNeverUpdated.Global
	public List<int> Elements = new();

	// TODO: DrawState/ParentAddon condition?

	public void Reset()
	{
		Enabled = false;
		Description = "";
		Position = Vector2.Zero;
		Size = new(300, 100);
		Anchor = DrawAnchor.Center;
		Elements.Clear();
	}

	public InteractableAreaConfig()
	{
		Reset();
	}

	public new static InteractableAreaConfig DefaultConfig() => new();
}