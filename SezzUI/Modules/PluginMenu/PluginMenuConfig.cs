using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Enums;
using SezzUI.Modules.PluginMenu;

namespace SezzUI.Interface.GeneralElements
{
	[DisableParentSettings("Size")]
	[Section("Plugin Menu")]
	[SubSection("General", 0)]
	public class PluginMenuConfig : AnchorablePluginConfigObject
	{
		[NestedConfig("Item 1", 21)]
		public PluginMenuItemConfig Item1 = new();

		[NestedConfig("Item 2", 22)]
		public PluginMenuItemConfig Item2 = new();

		[NestedConfig("Item 3", 23)]
		public PluginMenuItemConfig Item3 = new();

		[NestedConfig("Item 4", 24)]
		public PluginMenuItemConfig Item4 = new();

		[NestedConfig("Item 5", 25)]
		public PluginMenuItemConfig Item5 = new();

		[NestedConfig("Item 6", 26)]
		public PluginMenuItemConfig Item6 = new();

		[NestedConfig("Item 7", 27)]
		public PluginMenuItemConfig Item7 = new();

		[NestedConfig("Item 8", 28)]
		public PluginMenuItemConfig Item8 = new();

		[NestedConfig("Item 9", 29)]
		public PluginMenuItemConfig Item9 = new();

		[NestedConfig("Item 10", 30)]
		public PluginMenuItemConfig Item10 = new();

		[JsonIgnore]
		public List<PluginMenuItemConfig> Items;

		public void Reset()
		{
			Enabled = true;
			Anchor = DrawAnchor.BottomRight;
			Size = new(300, 30);
			Position = new(-9, -19);
			Items.ForEach(item => item.Reset());

			// SezzUI
			Item1.Enabled = true;
			Item1.Type = ItemType.SezzUI;
			Item1.Command = "/sezzui";
			Item1.Title = "Sezz|cFFFFFFFFUI";
			Item1.Color.Vector = new(1f / 255f, 182f / 255f, 214f / 255f, 255f / 255f);
		}

		public PluginMenuConfig()
		{
			Items = new() {Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10};
			Reset();
		}

		public new static PluginMenuConfig DefaultConfig() => new();
	}

	public class PluginMenuItemConfig : PluginConfigObject
	{
		[Combo("Type", "SezzUI Configuration", "Chat Command", isMonitored = true)]
		[Order(1)]
		public ItemType Type = 0;

		[InputText("Command", formattable = false, help = "Only for custom \"Chat Command\" items, will be ignored otherwise.")]
		[Order(2)]
		public string Command = "";

		[InputText("Title", formattable = false, help = "Text/Icon ::IconFile - has to be in the plugin media folder, example: ::Images\\Icon.png\nText color can be changed using |cAARRGGBB notation.", isMonitored = true)]
		[Order(3)]
		public string Title = "";

		[InputText("Tooltip", formattable = false)]
		[Order(4)]
		public string Tooltip = "";

		[ColorEdit4("Color")]
		[Order(7)]
		public PluginConfigColor Color = new(Vector4.One); // White

		[Checkbox("Toggle Color On Click/Enable", isMonitored = true)]
		[Order(10)]
		public bool Toggleable;

		[ColorEdit4("Toggle Color")]
		[Order(4, collapseWith = nameof(Toggleable))]
		public PluginConfigColor ColorToggled = new(Vector4.One); // White

		[InputText("Plugin Name", formattable = false, isMonitored = true)]
		[Order(5, collapseWith = nameof(Toggleable))]
		public string PluginToggleName = "";

		[InputText("Plugin Enabled Property", formattable = false, isMonitored = true, help = "Boolean property to check using reflection.")]
		[Order(6, collapseWith = nameof(Toggleable))]
		public string PluginToggleProperty = "";

		public void Reset()
		{
			Enabled = false;
			Type = ItemType.ChatCommand;
			Command = "";
			Title = "";
			Tooltip = "";
			Color.Vector = Vector4.One; // White
			Toggleable = false;
			ColorToggled.Vector = Vector4.One; // White
			PluginToggleName = "";
			PluginToggleProperty = "";
		}

		public PluginMenuItemConfig()
		{
			Reset();
		}

		public new static PluginMenuItemConfig DefaultConfig() => new();
	}
}