// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/AddonActionBarBase.cs

using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SezzUI.Modules.GameUI;

[StructLayout(LayoutKind.Explicit, Size = 0x258)]
public struct AddonActionBarBase
{
	[FieldOffset(0x000)]
	public AtkUnitBase AtkUnitBase;

	[FieldOffset(0x24C)]
	public byte HotbarID; // 12 Mount/QuestVehicle, 18 Praetorium Magitek

	[FieldOffset(0x250)]
	public byte IsShared;

	[FieldOffset(0x255)]
	public byte HasPetHotbar;

	[FieldOffset(0x280)]
	public byte LayoutID;

	public ActionBarLayout Layout => Enum.IsDefined(typeof(ActionBarLayout), LayoutID) ? (ActionBarLayout) LayoutID : ActionBarLayout.Unknown;
}

// ActionBar Agent offset 0xDE seems to be the page that receives key events?