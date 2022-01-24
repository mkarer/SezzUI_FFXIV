using System.Collections.Generic;
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

		private PluginMenuConfig()
		{
			Items = new() {Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10};
		}

		public new static PluginMenuConfig DefaultConfig() =>
			new()
			{
				Enabled = true,
				Anchor = DrawAnchor.BottomRight,
				Size = new(300, 80),
				Position = new(-8, -22),
				Item1 = new() {Enabled = true, Type = ItemType.SezzUI}
			};
	}

	[Exportable(false)]
	public class PluginMenuItemConfig : PluginConfigObject
	{
		[Combo("Type", "SezzUI Configuration", "Chat Command")]
		[Order(1)]
		public ItemType Type = ItemType.ChatCommand;

		[InputText("Command", formattable = false, help = "Only for custom \"Chat Command\" items, will be ignored otherwise.")]
		[Order(2)]
		public string Command = "";

		[InputText("Title", formattable = false, help = "Text/Icon ::IconFile (has to a square image in Media\\Icons)).\nText color can be changed using |cAARRGGBB notation.")]
		[Order(3)]
		public string Title = "";

		[ColorEdit4("Text Color")]
		[Order(4)]
		public PluginConfigColor TextColor = new(new(255f / 255f, 255f / 255f, 255f / 255f, 100f / 100f));

		public PluginMenuItemConfig()
		{
			Enabled = false;
		}

		public new static PluginMenuItemConfig DefaultConfig() =>
			new()
			{
				Enabled = false
			};
	}
}