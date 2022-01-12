using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace DelvUI.Helpers
{
    internal class SpellHelper
    {
        #region Singleton
        private static Lazy<SpellHelper> _lazyInstance = new Lazy<SpellHelper>(() => new SpellHelper());

        public static SpellHelper Instance => _lazyInstance.Value;

        ~SpellHelper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _lazyInstance = new Lazy<SpellHelper>(() => new SpellHelper());
        }
        #endregion

        private readonly unsafe ActionManager* _actionManager;
        private readonly ClientState _clientState;

        private readonly Dictionary<uint, Dictionary<uint, uint>> _actionAdjustments;

        public unsafe SpellHelper()
        {
            _actionManager = ActionManager.Instance();
            _clientState = SezzUI.Plugin.ClientState;
            _actionAdjustments = new()
            {
                // Hardcoded values for GetAdjustedActionId for actions that might get replaced by
                // another plugin (combo plugins hook GetAdjustedActionId):

                // RPR
                { 24405, new() { { 72, 24405 } } }, // Arcane Circle
                { 24394, new() { { 80, 24394 } } }, // Enshroud
                // PLD
                { 3538, new() { { 54, 3538 } } }, // Goring Blade
                { 7383, new() { { 54, 7383 } } }, // Requiescat
                // MCH
                { 2864, new() { { 40, 2864 }, { 80, 16501 } } }, // Rook Autoturret/Automation Queen
                // BLM
                { 3573, new() { { 52, 3573 } } }, // Ley Lines
                // DNC
                { 15997 , new() { { 15, 15997 } } }, // Standard Step
                { 15998, new() { { 70, 15998 } } }, // Technical Step
                // SGE
                { 24293, new() { { 30, 24293 }, { 72, 24308 }, { 82, 24314 } } }, // Eukrasian Dosis
                // SMN
                { 3581, new() { { 58, 3581 }, { 70, 7427 } } },
                // BRD
                { 3559, new() { { 52, 3559 } } }, // The Wanderer's Minuet
            };
        }

        public unsafe uint GetSpellActionId(uint actionId)
        {
            byte level = _clientState.LocalPlayer?.Level ?? 0;
            uint actionIdAdjusted = _actionAdjustments.TryGetValue(actionId, out var actionAdjustments) ? actionAdjustments.Where(a => level >= a.Key).OrderByDescending(a => a.Key).Select(a => a.Value).FirstOrDefault() : 0;
            return actionIdAdjusted > 0 ? actionIdAdjusted : _actionManager->GetAdjustedActionId(actionId);
        }

        public unsafe float GetRecastTimeElapsed(uint actionId, ActionType actionType = ActionType.Spell) => _actionManager->GetRecastTimeElapsed(actionType, GetSpellActionId(actionId));

        public unsafe float GetRecastTime(uint actionId, ActionType actionType = ActionType.Spell) => _actionManager->GetRecastTime(actionType, GetSpellActionId(actionId));

        public unsafe void GetRecastTimes(uint actionId, out float total, out float elapsed, ActionType actionType = ActionType.Spell)
        {
            total = 0f;
            elapsed = 0f;

            int recastGroup = _actionManager->GetRecastGroup((int)actionType, actionId);
            RecastDetail* recastDetail = _actionManager->GetRecastGroupDetail(recastGroup);
            if (recastDetail != null)
            {
                total = recastDetail->Total;
                elapsed = total > 0 ? recastDetail->Elapsed : 0;
            }
            else
            {
                total = GetRecastTime(actionId, actionType);
                elapsed = total > 0 ? GetRecastTimeElapsed(actionId, actionType) : 0;
            }
        }

        public unsafe ushort GetMaxCharges(uint actionId, uint level)
        {
            return ActionManager.GetMaxCharges(actionId, level);
        }
    }
}
