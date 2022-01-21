using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.Game;
using SezzUI;
using OFM = SezzUI.Hooking.OriginalFunctionManager;

namespace DelvUI.Helpers
{
	internal class SpellHelper
	{
		#region Singleton

		private static Lazy<SpellHelper> _lazyInstance = new(() => new());

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

		private void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_lazyInstance = new(() => new());
		}

		#endregion

		private readonly unsafe ActionManager* _actionManager;
		private readonly ClientState _clientState;

		private readonly Dictionary<uint, Dictionary<uint, uint>> _actionAdjustments;

		public unsafe SpellHelper()
		{
			_actionManager = ActionManager.Instance();
			_clientState = Plugin.ClientState;
			_actionAdjustments = new()
			{
				// Hardcoded values for GetAdjustedActionId for actions that might get replaced by
				// another plugin (combo plugins hook GetAdjustedActionId):

				// RPR
				{24405, new() {{72, 24405}}}, // Arcane Circle
				{24394, new() {{80, 24394}}}, // Enshroud
				// PLD
				{3538, new() {{54, 3538}}}, // Goring Blade
				{7383, new() {{54, 7383}}}, // Requiescat
				// MCH
				{2864, new() {{40, 2864}, {80, 16501}}}, // Rook Autoturret/Automation Queen
				// BLM
				{3573, new() {{52, 3573}}}, // Ley Lines
				// DNC
				{15997, new() {{15, 15997}}}, // Standard Step
				{15998, new() {{70, 15998}}}, // Technical Step
				// SGE
				{24293, new() {{30, 24293}, {72, 24308}, {82, 24314}}}, // Eukrasian Dosis
				// SMN
				{3581, new() {{58, 3581}, {70, 7427}}},
				// BRD
				{3559, new() {{52, 3559}}}, // The Wanderer's Minuet
				// GNB
				{16161, new() {{68, 16161}, {82, 25758}}}, // Heart of Stone/Heart of Corundum
				// DRG
				{88, new() {{50, 88}, {86, 25772}}}, // Chaos Thrust
				{3555, new() {{60, 3555}}} // Geirskogul
			};
		}

		public uint GetSpellActionId(uint actionId)
		{
			byte level = _clientState.LocalPlayer?.Level ?? 0;
			uint actionIdAdjusted = _actionAdjustments.TryGetValue(actionId, out Dictionary<uint, uint>? actionAdjustments) ? actionAdjustments.Where(a => level >= a.Key).OrderByDescending(a => a.Key).Select(a => a.Value).FirstOrDefault() : 0;
			return actionIdAdjusted > 0 ? actionIdAdjusted : OFM.GetAdjustedActionId(actionId);
		}
	}
}