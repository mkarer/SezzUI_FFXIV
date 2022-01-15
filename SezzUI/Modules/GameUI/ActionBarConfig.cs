using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Modules.GameUI;
using Newtonsoft.Json;

namespace SezzUI.Interface.GeneralElements
{
    [DisableParentSettings("Enabled")]
    [Section("Game UI")]
    [SubSection("Action Bars", 0)]
    public class ActionBarConfig : PluginConfigObject
    {
        [Checkbox("Enable Bar Paging (Ctrl: Page 2, Alt: Page 3)", isMonitored = true)]
        [Order(5)]
        public bool EnableBarPaging = false;

        [NestedConfig("ActionBar 1", 60, collapsingHeader = false)]
        public SingleActionBarConfig Bar1 = new SingleActionBarConfig(Element.ActionBar1);

        [NestedConfig("ActionBar 2", 61, collapsingHeader = false)]
        public SingleActionBarConfig Bar2 = new SingleActionBarConfig(Element.ActionBar2);

        [NestedConfig("ActionBar 3", 62, collapsingHeader = false)]
        public SingleActionBarConfig Bar3 = new SingleActionBarConfig(Element.ActionBar3);

        [NestedConfig("ActionBar 4", 64, collapsingHeader = false)]
        public SingleActionBarConfig Bar4 = new SingleActionBarConfig(Element.ActionBar4);

        [NestedConfig("ActionBar 5", 65, collapsingHeader = false)]
        public SingleActionBarConfig Bar5 = new SingleActionBarConfig(Element.ActionBar5);

        [NestedConfig("ActionBar 6", 65, collapsingHeader = false)]
        public SingleActionBarConfig Bar6 = new SingleActionBarConfig(Element.ActionBar6);

        [NestedConfig("ActionBar 7", 66, collapsingHeader = false)]
        public SingleActionBarConfig Bar7 = new SingleActionBarConfig(Element.ActionBar7);

        [NestedConfig("ActionBar 8", 67, collapsingHeader = false)]
        public SingleActionBarConfig Bar8 = new SingleActionBarConfig(Element.ActionBar8);

        [NestedConfig("ActionBar 9", 68, collapsingHeader = false)]
        public SingleActionBarConfig Bar9 = new SingleActionBarConfig(Element.ActionBar9);

        [NestedConfig("ActionBar 10", 69, collapsingHeader = false)]
        public SingleActionBarConfig Bar10 = new SingleActionBarConfig(Element.ActionBar10);

        public new static ActionBarConfig DefaultConfig()
        {
            return new ActionBarConfig()
            {
                Enabled = true,
            };
        }
    }

    [Exportable(false)]
    public class SingleActionBarConfig : PluginConfigObject
    {
        [JsonIgnore] public Element Bar = Element.Unknown;
   
        [Checkbox("Invert Row Ordering" + "##MP", isMonitored = true)]
        [Order(5)]
        public bool InvertRowOrdering = false;

        public SingleActionBarConfig(Element bar) : base()
        {
            Bar = bar;
            Enabled = false;
        }
    }
}
