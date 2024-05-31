using Newtonsoft.Json;
using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Modules.GameUI;

[DisableParentSettings("Enabled")]
[Section("Game UI")]
[SubSection("Action Bars", 0)]
public class ActionBarConfig : PluginConfigObject
{
	[NestedConfig("ActionBar 1", 60, collapsingHeader = false)]
	public SingleActionBarConfig Bar1 = new(Addon.ActionBar1);

	[NestedConfig("ActionBar 2", 61, collapsingHeader = false)]
	public SingleActionBarConfig Bar2 = new(Addon.ActionBar2);

	[NestedConfig("ActionBar 3", 62, collapsingHeader = false)]
	public SingleActionBarConfig Bar3 = new(Addon.ActionBar3);

	[NestedConfig("ActionBar 4", 64, collapsingHeader = false)]
	public SingleActionBarConfig Bar4 = new(Addon.ActionBar4);

	[NestedConfig("ActionBar 5", 65, collapsingHeader = false)]
	public SingleActionBarConfig Bar5 = new(Addon.ActionBar5);

	[NestedConfig("ActionBar 6", 65, collapsingHeader = false)]
	public SingleActionBarConfig Bar6 = new(Addon.ActionBar6);

	[NestedConfig("ActionBar 7", 66, collapsingHeader = false)]
	public SingleActionBarConfig Bar7 = new(Addon.ActionBar7);

	[NestedConfig("ActionBar 8", 67, collapsingHeader = false)]
	public SingleActionBarConfig Bar8 = new(Addon.ActionBar8);

	[NestedConfig("ActionBar 9", 68, collapsingHeader = false)]
	public SingleActionBarConfig Bar9 = new(Addon.ActionBar9);

	[NestedConfig("ActionBar 10", 69, collapsingHeader = false)]
	public SingleActionBarConfig Bar10 = new(Addon.ActionBar10);

	[Checkbox("Enable Bar Paging", isMonitored = true)]
	[Order(5)]
	public bool EnableBarPaging;

	[Combo("CTRL", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9", "Page 10")]
	[Order(6, collapseWith = nameof(EnableBarPaging))]
	public int BarPagingPageCtrl = 5;

	[Combo("ALT", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9", "Page 10")]
	[Order(6, collapseWith = nameof(EnableBarPaging))]
	public int BarPagingPageAlt = 2;

	public void Reset()
	{
		Enabled = true;
		Bar1.Reset();
		Bar2.Reset();
		Bar3.Reset();
		Bar4.Reset();
		Bar5.Reset();
		Bar6.Reset();
		Bar7.Reset();
		Bar8.Reset();
		Bar9.Reset();
		Bar10.Reset();
		EnableBarPaging = true;
		BarPagingPageCtrl = 5;
		BarPagingPageAlt = 2;
	}

	public ActionBarConfig()
	{
		Reset();
	}

	public new static ActionBarConfig DefaultConfig() => new();
}

public class SingleActionBarConfig : PluginConfigObject
{
	[JsonIgnore]
	public Addon Bar;

	[Checkbox("Invert Row Ordering" + "##MP", isMonitored = true)]
	[Order(5)]
	public bool InvertRowOrdering;

	public void Reset()
	{
		Enabled = false;
		InvertRowOrdering = false;
	}

	public SingleActionBarConfig(Addon bar)
	{
		Reset();
		Bar = bar;
	}
}