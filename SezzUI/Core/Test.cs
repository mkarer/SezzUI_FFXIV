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
using System.Runtime.InteropServices;

namespace SezzUI.Core
{
    public static class Test
    {
        public static unsafe void RunTest()
        {
            try
            {
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[RunTest] Failed: {ex}");
            }
        }
    }
}
