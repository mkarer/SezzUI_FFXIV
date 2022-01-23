﻿using Newtonsoft.Json;
using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Modules.GameUI;

namespace SezzUI.Interface.GeneralElements
{
	[DisableParentSettings("Enabled")]
	[Section("Game UI")]
	[SubSection("Action Bars", 0)]
	public class ActionBarConfig : PluginConfigObject
	{
		[NestedConfig("ActionBar 1", 60, collapsingHeader = false)]
		public SingleActionBarConfig Bar1 = new(Element.ActionBar1);

		[NestedConfig("ActionBar 2", 61, collapsingHeader = false)]
		public SingleActionBarConfig Bar2 = new(Element.ActionBar2);

		[NestedConfig("ActionBar 3", 62, collapsingHeader = false)]
		public SingleActionBarConfig Bar3 = new(Element.ActionBar3);

		[NestedConfig("ActionBar 4", 64, collapsingHeader = false)]
		public SingleActionBarConfig Bar4 = new(Element.ActionBar4);

		[NestedConfig("ActionBar 5", 65, collapsingHeader = false)]
		public SingleActionBarConfig Bar5 = new(Element.ActionBar5);

		[NestedConfig("ActionBar 6", 65, collapsingHeader = false)]
		public SingleActionBarConfig Bar6 = new(Element.ActionBar6);

		[NestedConfig("ActionBar 7", 66, collapsingHeader = false)]
		public SingleActionBarConfig Bar7 = new(Element.ActionBar7);

		[NestedConfig("ActionBar 8", 67, collapsingHeader = false)]
		public SingleActionBarConfig Bar8 = new(Element.ActionBar8);

		[NestedConfig("ActionBar 9", 68, collapsingHeader = false)]
		public SingleActionBarConfig Bar9 = new(Element.ActionBar9);

		[NestedConfig("ActionBar 10", 69, collapsingHeader = false)]
		public SingleActionBarConfig Bar10 = new(Element.ActionBar10);

		[Checkbox("Enable Bar Paging", isMonitored = true)]
		[Order(5)]
		public bool EnableBarPaging = false;
		
		[Combo("CTRL", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9", "Page 10")]
		[Order(6, collapseWith = nameof(EnableBarPaging))]
		public int BarPagingPageCtrl = 5;

		[Combo("ALT", "Page 1", "Page 2", "Page 3", "Page 4", "Page 5", "Page 6", "Page 7", "Page 8", "Page 9", "Page 10")]
		[Order(6, collapseWith = nameof(EnableBarPaging))]
		public int BarPagingPageAlt = 2;

		public new static ActionBarConfig DefaultConfig() =>
			new()
			{
				Enabled = true
			};
	}

	[Exportable(false)]
	public class SingleActionBarConfig : PluginConfigObject
	{
		[JsonIgnore]
		public Element Bar = Element.Unknown;

		[Checkbox("Invert Row Ordering" + "##MP", isMonitored = true)]
		[Order(5)]
		public bool InvertRowOrdering = false;

		public SingleActionBarConfig(Element bar)
		{
			Bar = bar;
			Enabled = false;
		}
	}
}