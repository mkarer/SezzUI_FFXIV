using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.ClientState.JobGauge.Types;
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
using SezzUI.GameStructs;

namespace SezzUI.Core
{
    public static unsafe class Test
    {

        public static unsafe void RunTest()
        {
            try
            {
                Plugin.ChatGui.Print("Okay.");
                Plugin.ChatGui.Print(Helpers.SpellHelper.GetAdjustedActionId(25800u,true).ToString());
                Plugin.ChatGui.Print(Helpers.SpellHelper.GetAdjustedActionId(7427u,true).ToString());
                Plugin.ChatGui.Print(Helpers.SpellHelper.GetAdjustedActionId(7428u,true).ToString());
                Plugin.ChatGui.Print(Helpers.SpellHelper.GetAdjustedActionId(7429u,true).ToString());
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[RunTest] Failed: {ex}");
            }
        }
    }
}
