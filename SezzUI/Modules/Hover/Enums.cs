using System.Runtime.Serialization;

namespace SezzUI.Modules.Hover
{
    public enum Element
    {
        [EnumMember(Value = "_ActionBar")] ActionBar01,
        [EnumMember(Value = "_ActionBar01")] ActionBar02,
        [EnumMember(Value = "_ActionBar02")] ActionBar03,
        [EnumMember(Value = "_ActionBar03")] ActionBar04,
        [EnumMember(Value = "_ActionBar04")] ActionBar05,
        [EnumMember(Value = "_ActionBar05")] ActionBar06,
        [EnumMember(Value = "_ActionBar06")] ActionBar07,
        [EnumMember(Value = "_ActionBar07")] ActionBar08,
        [EnumMember(Value = "_ActionBar08")] ActionBar09,
        [EnumMember(Value = "_ActionBar09")] ActionBar10,
        [EnumMember(Value = "_Action*Cross")] CrossHotbar, // _ActionCross + _ActionDoubleCrossL + _ActionDoubleCrossR

        [EnumMember(Value = "JobHud*")] Job,
        [EnumMember(Value = "_CastBar")] CastBar,
        [EnumMember(Value = "_Exp")] ExperienceBar,
        [EnumMember(Value = "_BagWidget")] InventoryGrid,
        [EnumMember(Value = "_Money")] Currency,
        [EnumMember(Value = "ScenarioTree")] ScenarioGuide,
        [EnumMember(Value = "_ToDoList")] QuestLog,
        [EnumMember(Value = "_MainCommand")] MainMenu,
        [EnumMember(Value = "ChatLog*")] Chat,
        [EnumMember(Value = "_NaviMap")] Minimap,

        [EnumMember(Value = "TargetInfo*")] TargetInfo,
        [EnumMember(Value = "_PartyList")] PartyList,
        [EnumMember(Value = "_LimitBreak")] LimitBreak,
        [EnumMember(Value = "_ParameterWidget")] Parameters,
        [EnumMember(Value = "_Status")] Status,
        [EnumMember(Value = "_StatusCustom0")] StatusEnhancements,
        [EnumMember(Value = "_StatusCustom1")] StatusEnfeeblements,
        [EnumMember(Value = "_StatusCustom2")] StatusOther,
    }
}
