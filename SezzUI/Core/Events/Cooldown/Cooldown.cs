/*
 * LibSezzCooldown-16
 * Initial port with basic functionality...
 * 
 * Just like in World of Warcraft we don't care about the accumulated duration of all charges,
 * instead we only care about one charge.
 *
 * IMPORTANT: Only ActionType.Spell is supported, to watch "General" actions lookup their action ID first.
 * This can easily be done by enabling debug logging and using the action, SendAction and ReceiveActionEffect
 * should output the correct ID.
 *
 * TODO: Remove all unused action type code.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using JetBrains.Annotations;
using SezzUI.GameStructs;
using SezzUI.Helpers;

namespace SezzUI.GameEvents
{
	internal sealed unsafe class Cooldown : BaseGameEvent
	{
#pragma warning disable 67
		public delegate void CooldownChangedDelegate(uint actionId, CooldownData data, bool chargesChanged, ushort previousCharges);

		public event CooldownChangedDelegate? CooldownChanged;

		public delegate void CooldownStartedDelegate(uint actionId, CooldownData data);

		public event CooldownStartedDelegate? CooldownStarted;

		public delegate void CooldownFinishedDelegate(uint actionId, CooldownData data, uint elapsedFinish);

		public event CooldownFinishedDelegate? CooldownFinished;
#pragma warning restore 67

		// private delegate byte UseActionDelegate(IntPtr actionManager, ActionType actionType, uint actionId, long targetId, uint param, uint useType, int pvp, IntPtr a7);
		// private Hook<UseActionDelegate>? _useActionHook;
		//
		// private delegate byte UseActionLocationDelegate(IntPtr actionManager, byte actionType, uint actionId, long targetObjectId, IntPtr location, uint param);
		// private Hook<UseActionLocationDelegate>? _useActionLocationHook;

		private delegate void SendActionDelegate(long targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9);

		private Hook<SendActionDelegate>? _sendActionHook;

		private delegate void ReceiveActionEffectDelegate(int sourceActorId, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);

		private Hook<ReceiveActionEffectDelegate>? _receiveActionEffectHook;

		// TODO: Delay first update based on latency?
		// Also: https://twitter.com/perchbird_/status/1282734091780186120
		private static readonly uint UPDATE_DELAY_INITIAL = 1850;
		private static readonly uint UPDATE_DELAY_REPEATED = 5000;
		private static readonly uint UPDATE_DELAY_REPEATED_FREQUENT = 50; // 60 FPS = 16.7ms/Frame

		private readonly Dictionary<uint, ushort> _watchedActions = new();
		private readonly Dictionary<uint, CooldownData> _cache = new();

		private readonly List<Tuple<long, uint>> _delayedUpdates = new();
		private readonly DelayedCooldownUpdateComparer _delayedCooldownUpdateComparer = new();

		private static readonly Dictionary<uint, uint> _actionsModifyingCooldowns = new() // Actions that modify the duration of other cooldowns
		{
			// WAR
			// Enhanced Infuriate [157]: Reduces Infuriate [52] recast time by 5 seconds upon landing Inner Beast [49], Steel Cyclone [51], Fell Cleave [3549], or Decimate [3550] on most targets.
			{49, 52},
			{51, 52},
			{3549, 52},
			{3550, 52},
			// BRD
			// Empyreal Arrow [3885] -> Bloodletter [110] (and Rain of Death [117] which is grouped below) during Mage's Ballad
			{3558, 110}
			// SMN
			// XIVCombo Demi Enkindle Feature: Enkindle [7429] somehow is still 7427?
			//{ 7427, 7429 },
		};

		private static readonly List<List<uint>> _cooldownGroups = new() // Actions that share the same cooldown
		{
			// SAM
			new() {7867, 16483, 16486, 16485, 16484}, // (Iaijutsu (XIVCombo) [7867]) Tsubame-gaeshi [16483] => Kaeshi: Setsugekka [16486] + Kaeshi: Goken [16485] + Kaeshi: Higanbana [16484]
			// BRD
			new() {110, 117}, // Bloodletter [110] + Rain of Death [117]
			// RDM
			new() {16527, 7515}, // Engagement [16527] + Displacement [7515]
			// SMN
			new() {7427, 25831}, // Summon Bahamut [7427] + Summon Phoenix [25831]
			new() {7429, 16516}, // Enkindle Bahamut [7429] + Enkindle Phoenix [16516]
		};

		private static readonly List<uint> _frequentUpdateCooldowns = new() // Workaround until I figure out how to do this correctly. Yes, this is absolute bullshit :(
		{
			// BRD: Bloodletter [110], Rain of Death [117] => Mage's Ballad
			110,
			117
		};

		#region Public Methods

		public void Watch(uint actionId)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownUsage)
			{
				Logger.Debug("Watch", $"Action ID: {actionId}");
			}
#endif

			if (_watchedActions.ContainsKey(actionId))
			{
				_watchedActions[actionId]++;
			}
			else
			{
				_watchedActions[actionId] = 1;
				Update(actionId, ActionType.Spell);
			}
		}

		public void Unwatch(uint actionId, bool all = false)
		{
			if (!_watchedActions.ContainsKey(actionId))
			{
				return;
			}

#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownUsage)
			{
				Logger.Debug("Unwatch", $"Action ID: {actionId} All: {all}");
			}
#endif

			if (!all)
			{
				_watchedActions[actionId]--;
			}

			if (all || _watchedActions[actionId] == 0)
			{
				_watchedActions.Remove(actionId);
				_delayedUpdates.RemoveAll(x => x.Item2 == actionId);
				_cache.Remove(actionId);
			}
		}

		public CooldownData Get(uint actionId)
		{
			if (!_watchedActions.ContainsKey(actionId))
			{
				Logger.Error("Get", $"Error: Cannot retrieve data for unwatched cooldown! Action ID: {actionId}");
			}
			else if (_cache.ContainsKey(actionId))
			{
				return _cache[actionId];
			}

			return new();
		}

		#endregion

		#region Cooldown Data Update

		private bool Update(uint actionId, ActionType actionType)
		{
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
			if (player == null)
			{
				return false;
			}

			// We're not adjusting the action ID in here, because actionId should already be adjusted.
			GetRecastTimes(actionId, out float totalCooldown, out float totalElapsed, actionType);
			ushort maxChargesMaxLevel = GetMaxCharges(actionId, Constants.MAX_PLAYER_LEVEL);
			ushort maxChargesCurrentLevel = player.Level < Constants.MAX_PLAYER_LEVEL ? GetMaxCharges(actionId, player.Level) : maxChargesMaxLevel;
			float chargesMod = maxChargesCurrentLevel != maxChargesMaxLevel ? 1f / maxChargesMaxLevel * maxChargesCurrentLevel : 1;
			float totalCooldownAdjusted = totalCooldown * chargesMod; // GetRecastTime returns total cooldown for max level charges (but only if we have multiple charges right now?)

			//Logger.Debug($"maxChargesCurrentLevel {maxChargesCurrentLevel} maxChargesMaxLevel {maxChargesMaxLevel}");
			//Logger.Debug($"totalCooldown {totalCooldown} totalElapsed {totalElapsed} totalElapsedMS {(int)(totalElapsed * 1000f)} DateTime.UtcNow.Ticks {DateTime.UtcNow.Ticks} DateTime.UtcNow.Ticks-totalElapsed {DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(totalElapsed)).Ticks}");
			//Logger.Debug($"totalCooldownAdjusted {totalCooldownAdjusted} chargesMod {chargesMod}");

			bool isFinished = totalCooldownAdjusted <= 0;
			if (!isFinished)
			{
				bool cooldownStarted = !_cache.ContainsKey(actionId);
				CooldownData data = cooldownStarted ? new() : _cache[actionId];
				data.PrepareUpdate();

				float cooldownPerCharge = totalCooldownAdjusted / maxChargesCurrentLevel;
				float totalElapsedAdjusted = Math.Min(totalCooldownAdjusted, totalElapsed); // GetRecastTimeElapsed is weird when maxChargesCurrentLevel differs from maxChargesMaxLevel
				float chargingMilliseconds = totalElapsedAdjusted % cooldownPerCharge * 1000;

				data.MaxCharges = maxChargesCurrentLevel;
				data.Duration = (uint) cooldownPerCharge * 1000;

				ushort previousCharges = data.CurrentCharges;

				data.CurrentCharges = totalElapsedAdjusted > 0 || maxChargesMaxLevel == 1 ? (ushort) Math.Floor(totalElapsedAdjusted / cooldownPerCharge) : maxChargesCurrentLevel;

				bool chargesChanged = !cooldownStarted && data.CurrentCharges != previousCharges;

				if (chargingMilliseconds > 0)
				{
					data.StartTime = Environment.TickCount64 - (int) chargingMilliseconds; // Not 100% accurate for some reason, should be fine though (if not we can still update more often).
				}

				data.Type = actionType;

				//Logger.Debug($"now {Environment.TickCount64}");
				//Logger.Debug($"totalCooldownAdjusted {totalCooldownAdjusted}");
				//Logger.Debug($"totalElapsedAdjusted {totalElapsedAdjusted}");
				//Logger.Debug($"oneChargeCooldown {totalElapsedAdjusted % cooldownPerCharge}");
				//Logger.Debug($"totalCooldown {totalCooldown}");
				//Logger.Debug($"chargesMod {chargesMod}");
				//Logger.Debug($"StartTime {data.StartTime} ElapsedCalculated {Environment.TickCount64 - data.StartTime}ms elapsed {totalElapsedAdjusted % cooldownPerCharge}s {(totalElapsedAdjusted % cooldownPerCharge) * 1000}ms {(long)((totalElapsedAdjusted % cooldownPerCharge) * 1000)}ms ");

				isFinished = !data.IsActive;
				if (!isFinished)
				{
					// Schedule update
					uint delay = _frequentUpdateCooldowns.Contains(actionId) ? UPDATE_DELAY_REPEATED_FREQUENT : cooldownStarted ? UPDATE_DELAY_INITIAL : UPDATE_DELAY_REPEATED;
					uint remaining = data.Remaining;
					ScheduleUpdate(actionId, remaining >= delay ? delay : remaining + 1);

					if (cooldownStarted)
					{
						// New
						InvokeCooldownStarted(actionId, data);
						_cache[actionId] = data;
					}
					else
					{
						// Updated
						if (data.HasChanged)
						{
							InvokeCooldownChanged(actionId, data, chargesChanged, previousCharges);
						}
					}

					return true;
				}
			}

			if (isFinished)
			{
				// Inactive
				if (_cache.ContainsKey(actionId))
				{
					CooldownData data = _cache[actionId];
					data.CurrentCharges = data.MaxCharges;
					//Logger.Debug("Update", $"ActionID {actionId} FinishedBecauseTotalCooldownAdjusted {totalCooldownAdjusted <= 0} Now {Environment.TickCount64} StartTime {data.StartTime} Duration {data.Duration} Elapsed {Environment.TickCount64 - data.StartTime - data.Duration}");
					long elapsedFinished = Environment.TickCount64 - data.StartTime - data.Duration;
					InvokeCooldownFinished(actionId, data, elapsedFinished < 0 ? 0 : (uint) elapsedFinished); // elapsedFinished can be negative!
					_cache.Remove(actionId);
				}
			}

			return false;
		}

		private void UpdateAll()
		{
			foreach (KeyValuePair<uint, ushort> kvp in _watchedActions)
			{
				Update(kvp.Key, ActionType.Spell);
			}
		}

		private bool TryUpdateIfWatched(uint actionId, ActionType actionType, [UsedImplicitly] bool isAdjusted = false, bool isGroupItem = false, bool isModifyingAction = false)
		{
			bool success = false;

			if (_watchedActions.ContainsKey(actionId))
			{
				// Action should be on cooldown now!
				if (!Update(actionId, actionType))
				{
					// Apparently it's not.
					ScheduleMultipleUpdates(actionId);
					//Logger.Debug($"ScheduleMultipleUpdates/1: {actionId} {SpellHelper.GetActionName(actionId) ?? "?"}");
				}

				success = true;
			}

			if (!isGroupItem && !isModifyingAction && _actionsModifyingCooldowns.ContainsKey(actionId))
			{
				// Delay update, cooldown won't get changed instantly.
				TryUpdateIfWatched(_actionsModifyingCooldowns[actionId], actionType, isAdjusted, isGroupItem, true);
				ScheduleMultipleUpdates(_actionsModifyingCooldowns[actionId]);
				//Logger.Debug($"ScheduleMultipleUpdates/2: {actionId} {SpellHelper.GetActionName(actionId) ?? "?"}");
				success = true;
			}

			if (!isGroupItem)
			{
				// Check if action is in a group and update watched group cooldowns
				foreach (List<uint> group in _cooldownGroups.Where(cdg => cdg.Contains(actionId)))
				{
					group.ForEach(groupedActionId => { TryUpdateAdjusted(groupedActionId, actionType, true); });
					success |= true;
				}
			}

			return success;
		}

		private bool TryUpdateAdjusted(uint actionId, ActionType actionType, bool isGroupItem = false)
		{
			if (!TryUpdateIfWatched(actionId, actionType, false, isGroupItem))
			{
				uint actionIdAdjusted = SpellHelper.GetAdjustedActionId(actionId);
				if (actionIdAdjusted != actionId)
				{
					return TryUpdateIfWatched(actionIdAdjusted, actionType, true, isGroupItem);
				}
			}
			else
			{
				return true;
			}

			return false;
		}

		#endregion

		#region Update Scheduling

		/// <summary>
		///     Schedule cooldown data update (in OnFrameworkUpdate)
		/// </summary>
		/// <param name="actionId"></param>
		/// <param name="delay">Minimum delay in milliseconds</param>
		private void ScheduleUpdate(uint actionId, uint delay)
		{
			lock (_delayedUpdates)
			{
				_delayedUpdates.Add(new(Environment.TickCount64 + delay, actionId));
				_delayedUpdates.Sort(_delayedCooldownUpdateComparer);
				//_delayedUpdates.ForEach(x => Logger.Debug("ScheduleUpdate", $"_delayedUpdates {x.Item1} {x.Item2}"));
			}
		}

		private void ScheduleMultipleUpdates(uint actionIds)
		{
			// TODO: Latency-based/Action-queue time?
			ScheduleUpdate(actionIds, 25);
			ScheduleUpdate(actionIds, 50);
			ScheduleUpdate(actionIds, 75);
			ScheduleUpdate(actionIds, 150);
			ScheduleUpdate(actionIds, 300);
			ScheduleUpdate(actionIds, 500);
			ScheduleUpdate(actionIds, 600);
			ScheduleUpdate(actionIds, 900);
			ScheduleUpdate(actionIds, 2500);
		}

		private void OnFrameworkUpdate(Framework framework)
		{
			if (_delayedUpdates.Any())
			{
				while (_delayedUpdates.Any() && _delayedUpdates[^1].Item1 <= Environment.TickCount64)
				{
					uint actionId = _delayedUpdates[^1].Item2;
					//Logger.Debug("OnFrameworkUpdate", $"TickCount64 {Environment.TickCount64} UpdateTickCount64 {_delayedUpdates[^1].Item1} ActionID {actionId}");
					_delayedUpdates.RemoveAt(_delayedUpdates.Count - 1);
					Update(actionId, ActionType.Spell);
				}
			}
		}

		/*
		private void OnActionEffect(uint actorId, uint actionId)
		{
			// Probably not needed anymore because of ReceiveActionEffect
			if (actorId != Plugin.ClientState.LocalPlayer?.ObjectId)
			{
				return;
			}

#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
			{
				Logger.Debug("OnActionEffect", $"Action ID: {actionId}");
			}
#endif

			TryUpdateIfWatched(actionId, GetActionType(actionId));
		}
		*/

		#endregion

		#region ActionManager

		private readonly ActionManager* _actionManager;

		private void GetRecastTimes(uint actionId, out float total, out float elapsed, ActionType actionType = ActionType.Spell)
		{
			total = 0f;
			elapsed = 0f;

			int recastGroup = _actionManager->GetRecastGroup((int) actionType, actionId);
			RecastDetail* recastDetail = _actionManager->GetRecastGroupDetail(recastGroup);
			if (recastDetail != null)
			{
				total = recastDetail->Total;
				elapsed = total > 0 ? recastDetail->Elapsed : 0;
			}
		}

		public static ushort GetMaxCharges(uint actionId, uint level = 0) => ActionManager.GetMaxCharges(actionId, level);

		#endregion

		#region Cooldown Events

		private void InvokeCooldownStarted(uint actionId, CooldownData data)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownStarted)
			{
				Logger.Debug("CooldownStarted", $"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms");
			}
#endif
			try
			{
				CooldownStarted?.Invoke(actionId, data);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "CooldownStarted", $"Failed invoking {nameof(CooldownStarted)}: {ex}");
			}
		}

		private void InvokeCooldownChanged(uint actionId, CooldownData data, bool chargesChanged, ushort previousCharges)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownChanged)
			{
				Logger.Debug("CooldownChanged", $"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} (Charges Changed: {chargesChanged}{(chargesChanged ? $" Previously: {previousCharges}" : "")}) Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms");
			}
#endif
			try
			{
				CooldownChanged?.Invoke(actionId, data, chargesChanged, previousCharges);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "CooldownChanged", $"Failed invoking {nameof(CooldownChanged)}: {ex}");
			}
		}

		private void InvokeCooldownFinished(uint actionId, CooldownData data, uint elapsedFinish)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownFinished)
			{
				Logger.Debug("CooldownFinished", $"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms ElapsedFinished {elapsedFinish}ms");
			}
#endif
			try
			{
				CooldownFinished?.Invoke(actionId, data, elapsedFinish);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "CooldownFinished", $"Failed invoking {nameof(CooldownFinished)}: {ex}");
			}
		}

		#endregion

		#region Hook

		private bool HookUseAction()
		{
			try
			{
				/*
				string? useActionSig = AsmHelper.GetSignature<ActionManager>("UseAction");
				if (useActionSig != null && Plugin.SigScanner.TryScanText(useActionSig, out var useActionPtr))
				{
					_useActionHook = new(useActionPtr, UseActionDetour);
#if DEBUG
					if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
					{
						Logger.Debug($"Hooked: UseAction (ptr = {useActionPtr.ToInt64():X})");
					}
#endif
				}
				else
				{
					Logger.Error($"Signature not found: UseAction");
				}
				*/

				if (Plugin.SigScanner.TryScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? F3 0F 10 3D ?? ?? ?? ?? 48 8D 4D BF", out IntPtr sendActionPtr))
				{
					_sendActionHook = new(sendActionPtr, SendActionDetour);
#if DEBUG
					if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
					{
						Logger.Debug($"Hooked: SendAction (ptr = {sendActionPtr.ToInt64():X})");
					}
#endif
					return true;
				}

				Logger.Error("Signature not found: SendAction");

				/*
				if (Plugin.SigScanner.TryScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 81 FB FB 1C 00 00", out var useActionLocationPtr))
				{
					_useActionLocationHook = new(useActionLocationPtr, UseActionLocationDetour);
#if DEBUG
					if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
					{
						Logger.Debug($"Hooked: UseActionLocation (ptr = {useActionLocationPtr.ToInt64():X})");
					}
#endif
				}
				else
				{
					Logger.Error($"Signature not found: SendAction");
				}
			*/
			}
			catch (Exception ex)
			{
				Logger.Error(ex, $"Failed to setup action hooks: {ex}");
			}

			return false;
		}

// 		private byte UseActionLocationDetour(IntPtr actionManager, byte actionType, uint actionId, long targetObjectId, IntPtr location, uint param)
// 		{
// 			var ret = _useActionLocationHook!.Original(actionManager, actionType, actionId, targetObjectId, location, param);
// #if DEBUG
// 			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
// 			{
// 				Logger.Debug("UseActionLocationDetour", $"Result: {ret} Action ID: {actionId} Action Type: {actionType} Target: 0x{targetObjectId:X} ({Plugin.ObjectTable.SearchById((uint)targetObjectId)?.Name.TextValue ?? "??"})");
// 			}
// #endif
// 			return ret;
// 		}

		private void SendActionDetour(long targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9)
		{
			_sendActionHook!.Original(targetObjectId, actionType, actionId, sequence, a5, a6, a7, a8, a9);
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
			{
				Logger.Debug("SendActionDetour", $"Action ID: {actionId} Type: {actionType} Name: {((ActionType) actionType == ActionType.Spell ? SpellHelper.GetActionName(actionId) ?? "?" : "?")} Target: 0x{targetObjectId:X} ({Plugin.ObjectTable.SearchById((uint) targetObjectId)?.Name.TextValue ?? "??"})");
			}
#endif

			if ((ActionType) actionType != ActionType.Spell || targetObjectId != Plugin.ClientState.LocalPlayer?.ObjectId)
			{
				return;
			}

			TryUpdateIfWatched(actionId, (ActionType) actionType);
		}

		/*
		private byte UseActionDetour(IntPtr actionManager, ActionType actionType, uint actionId, long targetedActorId, uint param, uint useType, int pvp, IntPtr a7)
		{
			var ret = _useActionHook!.Original(actionManager, actionType, actionId, targetedActorId, param, useType, pvp, a7);

#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
			{
				Logger.Debug("UseActionDetour", $"Result: {ret} Action ID: {actionId} Action Type: {actionType}");
			}
#endif

			try
			{
				if (ret == 1)
				{
					TryUpdateAdjusted(actionId, actionType);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "UseActionDetour", $"Error: {ex}");
			}

			return ret;
		}
		*/

		private bool HookReceiveActionEffect()
		{
			try
			{
				if (Plugin.SigScanner.TryScanText("E8 ?? ?? ?? ?? 48 8B 8D F0 03 00 00", out IntPtr receiveActionEffectPtr))
				{
					_receiveActionEffectHook = new(receiveActionEffectPtr, ReceiveActionEffectDetour);
#if DEBUG
					if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
					{
						Logger.Debug($"Hooked: ReceiveActionEffect (ptr = {receiveActionEffectPtr.ToInt64():X})");
					}
#endif
					return true;
				}

				Logger.Error("Signature not found: ReceiveActionEffect");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, $"Failed to setup hooks: {ex}");
			}

			return false;
		}

		private void ReceiveActionEffectDetour(int sourceActorId, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
		{
			_receiveActionEffectHook!.Original(sourceActorId, sourceActor, vectorPosition, effectHeader, effectArray, effectTrail);

			if (sourceActorId != Plugin.ClientState.LocalPlayer?.ObjectId || effectHeader == IntPtr.Zero)
			{
				return;
			}

			try
			{
				ActionEffectHeader header = Marshal.PtrToStructure<ActionEffectHeader>(effectHeader);
#if DEBUG
				if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
				{
					Logger.Debug("ReceiveActionEffectDetour", $"Action ID: {header.ActionId} Type: {header.Type} Name: {((ActionType) header.Type == ActionType.Spell ? SpellHelper.GetActionName(header.ActionId) ?? "?" : "?")} Source: 0x{sourceActorId:X} ({Plugin.ObjectTable.SearchById((uint) sourceActorId)?.Name.TextValue ?? "??"}) Target: 0x{header.TargetObjectId:X} ({Plugin.ObjectTable.SearchById((uint) header.TargetObjectId)?.Name.TextValue ?? "??"})");
				}
#endif
				if ((ActionType) header.Type == ActionType.Spell)
				{
					TryUpdateIfWatched(header.ActionId, (ActionType) header.Type);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "ReceiveActionEffectDetour", $"Error: {ex}");
			}
		}

		#endregion

		#region Event Toggle

		public override bool Enable()
		{
			if (base.Enable())
			{
				Plugin.Framework.Update += OnFrameworkUpdate;
				// EventManager.CombatLog.ActionEffect += OnActionEffect;
				// _useActionHook?.Enable();
				_receiveActionEffectHook?.Enable();
				_sendActionHook?.Enable();
				// _useActionLocationHook?.Enable();

				return true;
			}

			return false;
		}

		public override bool Disable()
		{
			if (base.Disable())
			{
				Plugin.Framework.Update -= OnFrameworkUpdate;
				// EventManager.CombatLog.ActionEffect -= OnActionEffect;
				// _useActionHook?.Disable();
				_receiveActionEffectHook?.Disable();
				_sendActionHook?.Disable();
				// _useActionLocationHook?.Disable();
				_delayedUpdates.Clear();
				_cache.Clear();

				return true;
			}

			return false;
		}

		#endregion

		#region Singleton

		private static readonly Lazy<Cooldown> _ev = new(() => new());
		public static Cooldown Instance => _ev.Value;
		public static bool Initialized => _ev.IsValueCreated;

		public Cooldown()
		{
			_actionManager = ActionManager.Instance();
		}

		protected override void Initialize()
		{
			HookUseAction(); // TODO: Don't allow enabling if this fails, because it doesn't do shit without the hook.
			HookReceiveActionEffect();
			base.Initialize();
		}

		protected override void InternalDispose()
		{
			_watchedActions.Clear();
			_actionsModifyingCooldowns.Clear();
			_cooldownGroups.Clear();
			_frequentUpdateCooldowns.Clear();
			// _useActionHook?.Dispose();
			_receiveActionEffectHook?.Dispose();
			_sendActionHook?.Dispose();
			// _useActionLocationHook?.Dispose();
		}

		#endregion
	}

	internal class DelayedCooldownUpdateComparer : IComparer<Tuple<long, uint>>
	{
		public int Compare(Tuple<long, uint>? x, Tuple<long, uint>? y)
		{
			if (x == null && y == null)
			{
				return 0;
			}

			if (x == null)
			{
				return 1;
			}

			if (y == null)
			{
				return -1;
			}

			return y.Item1.CompareTo(x.Item1);
		}
	}
}