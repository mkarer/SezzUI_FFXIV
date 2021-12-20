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
                // PLD
                { 3538, new() { { 54, 3538} } }, // Goring Blade
                { 7383, new() { { 54, 7383 } } }, // Requiescat
                // MCH
                { 2864 , new() { { 40, 2864 }, { 80, 16501 } } }, // Rook Autoturret/Automation Queen
            };
        }

        public unsafe uint GetSpellActionId(uint actionId) {
            byte level = _clientState.LocalPlayer?.Level ?? 0;
            uint actionIdAdjusted = _actionAdjustments.TryGetValue(actionId, out var actionAdjustments) ? actionAdjustments.Where(a => level >= a.Key).OrderByDescending(a => a.Key).Select(a => a.Value).FirstOrDefault() : 0;
            return actionIdAdjusted > 0 ? actionIdAdjusted : _actionManager->GetAdjustedActionId(actionId);
        }

        public unsafe float GetRecastTimeElapsed(uint actionId) => _actionManager->GetRecastTimeElapsed(ActionType.Spell, GetSpellActionId(actionId));

        public unsafe float GetRecastTime(uint actionId) => _actionManager->GetRecastTime(ActionType.Spell, GetSpellActionId(actionId));

        public float GetSpellCooldown(uint actionId) => Math.Abs(GetRecastTime(GetSpellActionId(actionId)) - GetRecastTimeElapsed(GetSpellActionId(actionId)));

        public int GetSpellCooldownInt(uint actionId)
        {
            if ((int)Math.Ceiling(GetSpellCooldown(actionId) % GetRecastTime(actionId)) <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(GetSpellCooldown(actionId) % GetRecastTime(actionId));
        }

        public int GetStackCount(int maxStacks, uint actionId)
        {
            if (GetSpellCooldownInt(actionId) == 0 || GetSpellCooldownInt(actionId) < 0)
            {
                return maxStacks;
            }

            return maxStacks - (int)Math.Ceiling(GetSpellCooldownInt(actionId) / (GetRecastTime(actionId) / maxStacks));
        }

        public unsafe ushort GetMaxCharges(uint actionId, uint level)
        {
            return ActionManager.GetMaxCharges(actionId, level);
        }

        /*public unsafe uint CheckActionResources(uint ActionID)
        {
            return _actionManager->CheckActionResources(ActionType.Spell, ActionID);
        }*/
    }
}
