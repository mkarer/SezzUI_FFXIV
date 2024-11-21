// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/AddonActionBarBase.cs
// 2024-11-21: Seems like nowadays only the LayoutID is missing in FFXIVClientStructs, might be time to create a PR...

using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SezzUI.Modules.GameUI;

[StructLayout(LayoutKind.Explicit, Size = 0x260)]
public struct AddonActionBarBase
{
	[FieldOffset(0x000)]
	public AtkUnitBase AtkUnitBase;

	[FieldOffset(0x254)]
	public byte RaptureHotbarId; // 12 Mount/QuestVehicle, 18 Praetorium Magitek

	[FieldOffset(0x258)]
	public byte IsSharedHotbar;

	[FieldOffset(0x255)]
	public byte DisplayPetBar;

	[FieldOffset(0x288)]
	public byte LayoutID;

	public ActionBarLayout Layout => Enum.IsDefined(typeof(ActionBarLayout), LayoutID) ? (ActionBarLayout) LayoutID : ActionBarLayout.Unknown;
}

// ActionBar Agent offset 0xDE seems to be the page that receives key events?