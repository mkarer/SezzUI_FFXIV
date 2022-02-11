using System;
using System.Collections.Generic;

namespace SezzUI.Modules.GameUI
{
	public enum Addon
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
		PetActionBar,

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
		StatusOther
	}

	public enum ActionBarLayout : byte
	{
		Unknown = 255,
		H12V1 = 0,
		H6V2 = 1,
		H4V3 = 2,
		H3V4 = 3,
		H2V6 = 4,
		H1V12 = 5
	}

	[Flags]
	public enum AddonVisibility : byte
	{
		UserHidden = 1 << 0, // Hidden by the user in current HUD layout
		GameHidden = 1 << 2 // Hidden by the game, Scenario Guide during duty for example
	}

	public static class Addons
	{
		public static readonly Dictionary<Addon, string> Names = new()
		{
			{Addon.ActionBar1, "_ActionBar"},
			{Addon.ActionBar2, "_ActionBar01"},
			{Addon.ActionBar3, "_ActionBar02"},
			{Addon.ActionBar4, "_ActionBar03"},
			{Addon.ActionBar5, "_ActionBar04"},
			{Addon.ActionBar6, "_ActionBar05"},
			{Addon.ActionBar7, "_ActionBar06"},
			{Addon.ActionBar8, "_ActionBar07"},
			{Addon.ActionBar9, "_ActionBar08"},
			{Addon.ActionBar10, "_ActionBar09"},
			{Addon.PetActionBar, "_ActionBarEx"},
			{Addon.CastBar, "_CastBar"},
			{Addon.ExperienceBar, "_Exp"},
			{Addon.InventoryGrid, "_BagWidget"},
			{Addon.Currency, "_Money"},
			{Addon.ScenarioGuide, "ScenarioTree"},
			{Addon.QuestLog, "_ToDoList"},
			{Addon.MainMenu, "_MainCommand"},
			{Addon.Minimap, "_NaviMap"},
			{Addon.PartyList, "_PartyList"},
			{Addon.LimitBreak, "_LimitBreak"},
			{Addon.Parameters, "_ParameterWidget"},
			{Addon.Status, "_Status"},
			{Addon.StatusEnhancements, "_StatusCustom0"},
			{Addon.StatusEnfeeblements, "_StatusCustom1"},
			{Addon.StatusOther, "_StatusCustom2"}
		};
	}
}