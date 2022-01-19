using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Modules.GameUI;

namespace SezzUI.GameStructs
{
	[StructLayout(LayoutKind.Explicit, Size = 0x248)]
	public struct AddonActionBarBase
	{
		[FieldOffset(0x000)]
		public AtkUnitBase AtkUnitBase;

		[FieldOffset(0x23C)]
		public byte HotbarID; // 12 Mount/QuestVehicle, 18 Praetorium Magitek

		[FieldOffset(0x240)]
		public byte IsShared;

		[FieldOffset(0x245)]
		public byte IsPetHotbar;

		[FieldOffset(0x270)]
		public byte LayoutID;

		public ActionBarLayout Layout => Enum.IsDefined(typeof(ActionBarLayout), LayoutID) ? (ActionBarLayout) LayoutID : ActionBarLayout.Unknown;
	}

	// ActionBar Agent offset 0xDE seems to be the page that receives key events?
}