using System.Collections.Generic;

namespace SezzUI.Modules.GameUI
{
    public enum Element
    {
        Unknown,

        ActionBar1 = 100,
        ActionBar2,
        ActionBar3,
        ActionBar4,
        ActionBar5,
        ActionBar6,
        ActionBar7,
        ActionBar8,
        ActionBar9,
        ActionBar10,
        ActionBarLock,
        CrossHotbar,

        Job = 200,
        CastBar,
        ExperienceBar,
        InventoryGrid,
        Currency,
        ScenarioGuide,
        QuestLog,
        MainMenu,
        Chat,
        Minimap,

        TargetInfo = 300,
        PartyList,
        LimitBreak,
        Parameters,
        Status,
        StatusEnhancements,
        StatusEnfeeblements,
        StatusOther,
    }

    public enum ActionBarLayout
    {
        Unknown,

        H12V1,
        H6V2,
        H4V3,
        H3V4,
        H2V6,
        H1V12
    }

    public static class Addons
    {
        public static readonly Dictionary<Element, string> Names = new()
        {
            { Element.ActionBar1, "_ActionBar" },
            { Element.ActionBar2, "_ActionBar01" },
            { Element.ActionBar3, "_ActionBar02" },
            { Element.ActionBar4, "_ActionBar03" },
            { Element.ActionBar5, "_ActionBar04" },
            { Element.ActionBar6, "_ActionBar05" },
            { Element.ActionBar7, "_ActionBar06" },
            { Element.ActionBar8, "_ActionBar07" },
            { Element.ActionBar9, "_ActionBar08" },
            { Element.ActionBar10, "_ActionBar09" },
            { Element.CastBar, "_CastBar" },
            { Element.ExperienceBar, "_Exp" },
            { Element.InventoryGrid, "_BagWidget" },
            { Element.Currency, "_Money" },
            { Element.ScenarioGuide, "ScenarioTree" },
            { Element.QuestLog, "_ToDoList" },
            { Element.MainMenu, "_MainCommand" },
            { Element.Minimap, "_NaviMap" },
            { Element.PartyList, "_PartyList" },
            { Element.LimitBreak, "_LimitBreak" },
            { Element.Parameters, "_ParameterWidget" },
            { Element.Status, "_Status" },
            { Element.StatusEnhancements, "_StatusCustom0" },
            { Element.StatusEnfeeblements, "_StatusCustom1" },
            { Element.StatusOther, "_StatusCustom2" },
        };
    }
}
