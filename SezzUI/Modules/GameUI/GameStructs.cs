﻿using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System.Reflection;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Diagnostics;

namespace SezzUI.GameStructs
{
    [StructLayout(LayoutKind.Explicit, Size = 0x248)]
    public unsafe struct AddonActionBarBase
    {
        [FieldOffset(0x000)] public AtkUnitBase AtkUnitBase;
        //[FieldOffset(0x220)] public ActionBarSlotAction* ActionBarSlotsAction;
        //[FieldOffset(0x228)] public void* UnknownPtr228; // Field of 0s
        //[FieldOffset(0x230)] public void* UnknownPtr230; // Points to same location as +0x228 ??
        //[FieldOffset(0x238)] public int UnknownInt238;
        [FieldOffset(0x23C)] public byte HotbarID;  // 12 = Vehicle
        //[FieldOffset(0x23D)] public sbyte HotbarIDOther;
        //[FieldOffset(0x23E)] public byte HotbarSlotCount;
        //[FieldOffset(0x23F)] public int UnknownInt23F;
        //[FieldOffset(0x243)] public int UnknownInt243; // Flags of some kind
        [FieldOffset(0x240)] public byte IsShared;
        [FieldOffset(0x245)] public byte IsPetHotbar;
    }

    //[StructLayout(LayoutKind.Explicit, Size = 0xC8)]
    //public unsafe struct ActionBarSlotAction
    //{
    //    [FieldOffset(0x04)] public int ActionId;       // Not cleared when slot is emptied
    //    [FieldOffset(0x18)] public void* UnknownPtr;   // Points 34 bytes ahead ??
    //    [FieldOffset(0x90)] public AtkComponentNode* Icon;
    //    [FieldOffset(0x98)] public AtkTextNode* ControlHintTextNode;
    //    [FieldOffset(0xA0)] public AtkResNode* IconFrame;
    //    [FieldOffset(0xA8)] public AtkImageNode* ChargeIcon;
    //    [FieldOffset(0xB0)] public AtkResNode* RecastOverlayContainer;
    //    [FieldOffset(0xB8)] public byte* PopUpHelpTextPtr; // Null when slot is empty
    //}
}
