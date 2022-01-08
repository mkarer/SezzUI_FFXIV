using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Logging;

namespace SezzUI.Core
{
    public static class Test
    {
        public static unsafe void RunTest()
        {
            try
            {
                //ActionManager* _actionManager = ActionManager.Instance();
                //PluginLog.Debug($"Action: 24383 (Gallows) Status: {_actionManager->GetActionStatus(ActionType.Spell, 24383)}");
                //PluginLog.Debug($"Action: 24382 (Gibbet) Status: {_actionManager->GetActionStatus(ActionType.Spell, 24382)}");

                RPRGauge gauge = Plugin.JobGauges.Get<RPRGauge>();
                PluginLog.Debug($"LemureShroud: {gauge?.LemureShroud ?? 0}");

                // 572 ready
                // 582 gcd
                // 579 ?! in cutscene
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[RunTest] Failed: {ex}");
            }
        }
    }
}
