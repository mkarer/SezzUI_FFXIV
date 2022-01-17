using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SezzUI.GameStructs
{
    [StructLayout(LayoutKind.Explicit, Size = 0x248)]
    public unsafe struct AddonActionBarBase
    {
        [FieldOffset(0x000)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(0x23C)] public byte HotbarID;  // 12 Mount/QuestVehicle, 18 Praetorium Magitek
        [FieldOffset(0x240)] public byte IsShared;
        [FieldOffset(0x245)] public byte IsPetHotbar;
    }

    // ActionBar Agent offset 0xDE seems to be the page that receives key events?
}
