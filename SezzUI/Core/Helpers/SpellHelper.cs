using System;
using System.Linq;
using Lumina.Excel;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaGeneralAction = Lumina.Excel.GeneratedSheets.GeneralAction;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using Dalamud.Utility;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;

namespace SezzUI.Helpers
{
	public class SpellHelper
	{
        private ExcelSheet<LuminaAction>? _sheetAction;
        private ExcelSheet<LuminaGeneralAction>? _sheetGeneralAction;
        private ExcelSheet<LuminaStatus>? _sheetStatus;

        public LuminaAction? GetAction(uint actionId) => _sheetAction?.GetRow(actionId);
        public string? GetActionName(uint actionId) => GetAction(actionId)?.Name.ToDalamudString().ToString();
        public ushort? GetActionIcon(uint actionId) => GetAction(actionId)?.Icon;

        public LuminaGeneralAction? GetGeneralAction(uint actionId) => _sheetGeneralAction?.GetRow(actionId);
        public string? GetGeneralActionName(uint actionId) => GetGeneralAction(actionId)?.Name.ToDalamudString().ToString();
        public int? GetGeneralActionIcon(uint actionId) => GetGeneralAction(actionId)?.Icon;

        public LuminaStatus? GetStatus(uint statusId) => _sheetStatus?.FirstOrDefault(status => status.RowId.Equals(statusId));

        public LuminaStatus? GetStatusByAction(uint actionId)
		{
			LuminaAction? action = GetAction(actionId);
			if (action != null)
			{
				if (_sheetStatus != null)
				{
					string actionName = action.Name.ToString().ToLower();
					LuminaStatus? status = _sheetStatus.FirstOrDefault(status => status.Name.ToString().ToLower().Equals(actionName));
					if (status != null)
					{
						return status;
					}
				}
			}

			return null;
		}

		public static unsafe BattleChara? GetUnit(Enums.Unit unit)
		{
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            if (player == null) { return null; }

			GameObject? target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
			GameObject? actor = unit switch
			{
				Enums.Unit.Player => player,
				Enums.Unit.Target => target,
				Enums.Unit.TargetOfTarget => DelvUI.Helpers.Utils.FindTargetOfTarget(player, target, Plugin.ObjectTable),
				Enums.Unit.FocusTarget => Plugin.TargetManager.FocusTarget,
				_ => null
			};

			if (actor is BattleChara)
			{
				return (BattleChara)actor;
			}

			return null;
		}

        public static unsafe Status? GetStatus(uint statusId, Enums.Unit unit, bool mustMatchPlayerSource = true)
		{
			if (unit == Enums.Unit.Any)
			{
				Status? status;
				foreach (Enums.Unit unitType in (Enums.Unit[])Enum.GetValues(typeof(Enums.Unit)))
				{
					if (unitType != unit)
					{
						status = GetStatus(statusId, unitType, mustMatchPlayerSource);
						if (status != null)
						{
							return status;
						}
					}
				}
			}
			else
			{
				BattleChara? actor = GetUnit(unit);
				if (actor != null)
				{
					PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
                    if (player == null) { return null; }

					foreach (var status in actor.StatusList)
					{
						if (status != null && status.StatusId == statusId && (!mustMatchPlayerSource || (mustMatchPlayerSource && status.SourceID == player.ObjectId)))
						{
							return status;
						}
					}
				}
			}

			return null;
		}

        public static unsafe Status? GetStatus(uint[] statusIds, Enums.Unit unit, bool mustMatchPlayerSource = true, bool priorizedByOrder = true)
		{
			if (unit == Enums.Unit.Any)
			{
				Status? status;
				foreach (Enums.Unit unitType in (Enums.Unit[])Enum.GetValues(typeof(Enums.Unit)))
				{
					if (unitType != unit)
					{
						status = GetStatus(statusIds, unitType, mustMatchPlayerSource, priorizedByOrder);
						if (status != null)
						{
							return status;
						}
					}
				}
			}
			else
			{
				BattleChara? actor = GetUnit(unit);
				if (actor != null)
				{
					PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
                    if (player == null) { return null; }

                    int bestIndex = -1;
                    Status? bestStatus = null;

					foreach (var status in actor.StatusList)
					{
                        if (status != null)
                        {
                            int foundIndex = Array.IndexOf(statusIds, status.StatusId);
                            if (foundIndex > -1 && (!mustMatchPlayerSource || (mustMatchPlayerSource && status.SourceID == player.ObjectId)) && (bestIndex == -1 || foundIndex < bestIndex))
                            {
                                bestIndex = foundIndex;
                                bestStatus = status;

                                if (!priorizedByOrder)
                                {
                                    return bestStatus;
                                }
                            }
                        }
					}

                    return bestStatus;
				}
			}

			return null;
		}

        #region Singleton
        private SpellHelper()
        {
            _sheetAction = Plugin.DataManager.Excel.GetSheet<LuminaAction>();
            _sheetGeneralAction = Plugin.DataManager.Excel.GetSheet<LuminaGeneralAction>();
            _sheetStatus = Plugin.DataManager.Excel.GetSheet<LuminaStatus>();
        }

        public static void Initialize() { Instance = new SpellHelper(); }

        public static SpellHelper Instance { get; private set; } = null!;

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

            Instance = null!;
        }
        #endregion
    }
}
