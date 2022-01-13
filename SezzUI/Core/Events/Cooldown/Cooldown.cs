/*
 * LibSezzCooldown-16
 * Initial port with basic functionality...
 * 
 * Just like in World of Warcraft we don't care about the accumulated duration of all charges,
 * instead we only care about one charge.
 */
using Dalamud.Hooking;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.GameEvents
{
    internal sealed unsafe class Cooldown : BaseGameEvent
    {
#pragma warning disable 67
        public delegate void CooldownChangedDelegate(uint actionId, CooldownData data);
        public event CooldownChangedDelegate? CooldownChanged;

        public delegate void CooldownStartedDelegate(uint actionId, CooldownData data);
        public event CooldownStartedDelegate? CooldownStarted;

        public delegate void CooldownFinishedDelegate(uint actionId, CooldownData data, uint elapsedFinish);
        public event CooldownFinishedDelegate? CooldownFinished;
#pragma warning restore 67

        private delegate byte UseActionDelegate(IntPtr actionManager, ActionType actionType, uint actionID, long targetID, uint param, uint useType, int pvp, IntPtr a7);
        private Hook<UseActionDelegate>? _useActionHook;

        private Dictionary<uint, ushort> _watchedActions = new();
        private Dictionary<uint, CooldownData> _cache = new();

        private List<Tuple<long, uint>> _delayedUpdates = new();
        private DelayedCooldownUpdateComparer _delayedCooldownUpdateComparer = new();

        private static Dictionary<uint, uint> _actionsModifyingCooldowns = new(){
            // Enhanced Infuriate [157]: Reduces Infuriate [52] recast time by 5 seconds upon landing Inner Beast [49], Steel Cyclone [51], Fell Cleave [3549], or Decimate [3550] on most targets.
            { 49, 52 },
            { 51, 52 },
            { 3549, 52 },
            { 3550, 52 },
        };

        // TODO: Delay first update based on latency?
        // Also: https://twitter.com/perchbird_/status/1282734091780186120
        private static uint UPDATE_DELAY_INITIAL = 1850;
        private static uint UPDATE_DELAY_REPEATED = 5000;

        public void Watch(uint actionId)
        {
#if DEBUG
            if (EventManager.Config.LogEvents && EventManager.Config.LogEventCooldownUsage)
            {
                LogDebug("Watch", $"Action ID: {actionId}");
            }
#endif

            if (_watchedActions.ContainsKey(actionId))
            {
                _watchedActions[actionId]++;
            }
            else
            {
                _watchedActions[actionId] = 1;
                Update(actionId, GetActionType(actionId));
            }
        }

        public void Unwatch(uint actionId, bool all = false)
        {
            if (!_watchedActions.ContainsKey(actionId)) { return; }

#if DEBUG
            if (EventManager.Config.LogEvents && EventManager.Config.LogEventCooldownUsage)
            {
                LogDebug("Unwatch", $"Action ID: {actionId} All: {all}");
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

        private void Update(uint actionId, ActionType actionType)
        {
            PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            if (player == null) { return; }

            // We're not adjusting the action ID in here, because actionId should already be adjusted.
            GetRecastTimes(actionId, out float totalCooldown, out float totalElapsed, actionType);
            ushort maxChargesMaxLevel = GetMaxCharges(actionId, Constants.MAX_PLAYER_LEVEL);
            ushort maxChargesCurrentLevel = player.Level < Constants.MAX_PLAYER_LEVEL ? GetMaxCharges(actionId, player.Level) : maxChargesMaxLevel;
            float chargesMod = maxChargesCurrentLevel != maxChargesMaxLevel ? 1f / maxChargesMaxLevel * maxChargesCurrentLevel : 1;
            float totalCooldownAdjusted = totalCooldown * chargesMod; // GetRecastTime returns total cooldown for max level charges (but only if we have multiple charges right now?)

            //LogDebug($"maxChargesCurrentLevel {maxChargesCurrentLevel} maxChargesMaxLevel {maxChargesMaxLevel}");
            //LogDebug($"totalCooldown {totalCooldown} totalElapsed {totalElapsed} totalElapsedMS {(int)(totalElapsed * 1000f)} DateTime.UtcNow.Ticks {DateTime.UtcNow.Ticks} DateTime.UtcNow.Ticks-totalElapsed {DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(totalElapsed)).Ticks}");
            //LogDebug($"totalCooldownAdjusted {totalCooldownAdjusted} chargesMod {chargesMod}");

            if (totalCooldownAdjusted > 0)
            {
                bool cooldownStarted = !_cache.ContainsKey(actionId);
                CooldownData data = cooldownStarted ? new() : _cache[actionId];
                data.PrepareUpdate();

                float cooldownPerCharge = totalCooldownAdjusted / maxChargesCurrentLevel;
                float totalElapsedAdjusted = Math.Min(totalCooldownAdjusted, totalElapsed); // GetRecastTimeElapsed is weird when maxChargesCurrentLevel differs from maxChargesMaxLevel
                float chargingMilliseconds = (totalElapsedAdjusted % cooldownPerCharge) * 1000;

                data.MaxCharges = maxChargesCurrentLevel;
                data.Duration = (uint)cooldownPerCharge * 1000;
                data.CurrentCharges = totalElapsedAdjusted > 0 || maxChargesMaxLevel == 1 ?
                    (ushort)Math.Floor(totalElapsedAdjusted / cooldownPerCharge) :
                    maxChargesCurrentLevel;
                
                data.StartTime = Environment.TickCount64 - (int)chargingMilliseconds; // Not 100% accurate for some reason, should be fine though (if not we can still update more often).
                data.Type = actionType;

                //LogDebug($"now {Environment.TickCount64}");
                //LogDebug($"totalCooldownAdjusted {totalCooldownAdjusted}");
                //LogDebug($"totalElapsedAdjusted {totalElapsedAdjusted}");
                //LogDebug($"oneChargeCooldown {totalElapsedAdjusted % cooldownPerCharge}");
                //LogDebug($"totalCooldown {totalCooldown}");
                //LogDebug($"chargesMod {chargesMod}");
                //LogDebug($"StartTime {data.StartTime} ElapsedCalculated {Environment.TickCount64 - data.StartTime}ms elapsed {totalElapsedAdjusted % cooldownPerCharge}s {(totalElapsedAdjusted % cooldownPerCharge) * 1000}ms {(long)((totalElapsedAdjusted % cooldownPerCharge) * 1000)}ms ");

                // Schedule update
                uint delay = cooldownStarted ? UPDATE_DELAY_INITIAL : UPDATE_DELAY_REPEATED;
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
                        InvokeCooldownChanged(actionId, data);
                    }
                }
            }
            else
            {
                // Inactive
                if (_cache.ContainsKey(actionId))
                {
                    CooldownData data = _cache[actionId];
                    data.CurrentCharges = data.MaxCharges;
                    InvokeCooldownFinished(actionId, data, (uint)(Environment.TickCount64 - data.StartTime) - data.Duration);
                    _cache.Remove(actionId);
                }
            }
        }

        private void UpdateAll()
        {
            foreach(var kvp in _watchedActions)
            {
                Update(kvp.Key, GetActionType(kvp.Key));
            }
        }

        public CooldownData Get(uint actionId) {
            if (!_watchedActions.ContainsKey(actionId)) {
                LogError("Get", $"Error: Cannot retrieve data for unwatched cooldown! Action ID: {actionId}");
            }
            else if (_cache.ContainsKey(actionId))
            {
                return _cache[actionId];
            }

            return new();
        }

        /// <summary>
        /// Schedule cooldown data update (in OnFrameworkUpdate)
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="delay">Minimum delay in milliseconds</param>
        private void ScheduleUpdate(uint actionId, uint delay)
        {
            lock(_delayedUpdates)
            {
                _delayedUpdates.Add(new(Environment.TickCount64 + delay, actionId));
                _delayedUpdates.Sort(_delayedCooldownUpdateComparer);
                //_delayedUpdates.ForEach(x => PluginLog.Debug($"_delayedUpdates {x.Item1} {x.Item2}"));
            }
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (_delayedUpdates.Any())
            {
                while (_delayedUpdates.Any() && _delayedUpdates[^1].Item1 <= Environment.TickCount64)
                {
                    uint actionId = _delayedUpdates[^1].Item2;
                    //LogDebug("OnFrameworkUpdate", $"TickCount64 {Environment.TickCount64} UpdateTickCount64 {_delayedUpdates[^1].Item1} ActionID {actionId}");
                    _delayedUpdates.RemoveAt(_delayedUpdates.Count - 1);
                    Update(actionId, GetActionType(actionId));
                }
            }
        }

        private ActionType GetActionType(uint actionId)
        {
            // TODO: Where do we find the correct ActionType?
            return _cache.ContainsKey(actionId) ?
                _cache[actionId].Type :
                (actionId == 4 || actionId == 8 ? ActionType.General : ActionType.Spell);
        }

        #region ActionManager
        private readonly unsafe ActionManager* _actionManager;

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
        }

        public unsafe ushort GetMaxCharges(uint actionId, uint level = 0)
        {
            return ActionManager.GetMaxCharges(actionId, level);
        }
        #endregion

        #region Cooldown Events
        private void InvokeCooldownStarted(uint actionId, CooldownData data)
        {
#if DEBUG
            if (EventManager.Config.LogEvents && EventManager.Config.LogEventCooldownStarted)
            {
                LogDebug("CooldownStarted", $"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms");
            }
#endif
            try
            {
                CooldownStarted?.Invoke(actionId, data);
            }
            catch (Exception ex)
            {
                LogError(ex, "CooldownStarted", $"Failed invoking {nameof(CooldownStarted)}: {ex}");
            }
        }

        private void InvokeCooldownChanged(uint actionId, CooldownData data)
        {
#if DEBUG
            if (EventManager.Config.LogEvents && EventManager.Config.LogEventCooldownChanged)
            {
                LogDebug("CooldownChanged", $"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms");
            }
#endif
            try
            {
                CooldownChanged?.Invoke(actionId, data);
            }
            catch (Exception ex)
            {
                LogError(ex, "CooldownChanged", $"Failed invoking {nameof(CooldownChanged)}: {ex}");
            }
        }

        private void InvokeCooldownFinished(uint actionId, CooldownData data, uint elapsedFinish)
        {
#if DEBUG
            if (EventManager.Config.LogEvents && EventManager.Config.LogEventCooldownFinished)
            {
                LogDebug("CooldownFinished", $"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms ElapsedFinished {elapsedFinish}ms");
            }
#endif
            try
            {
                CooldownFinished?.Invoke(actionId, data, elapsedFinish);
            }
            catch (Exception ex)
            {
                LogError(ex, "CooldownFinished", $"Failed invoking {nameof(CooldownFinished)}: {ex}");
            }
        }
        #endregion

        #region Hook
        private bool HookUseAction()
        {
            try
            {
                string? useActionSig = GetSignature<ActionManager>("UseAction");
                if (useActionSig != null && Plugin.SigScanner.TryScanText(useActionSig, out var useActionPtr))
                {
                    _useActionHook = new(useActionPtr, UseActionDetour);
#if DEBUG
                    LogDebug($"Hooked: UseAction (ptr = {useActionPtr.ToInt64():X})");
#endif
                    return true;
                }
                else
                {
                    LogError($"Signature not found: UseAction");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"Failed to setup hooks: {ex}");
            }

            return false;
        }

        private byte UseActionDetour(IntPtr actionManager, ActionType actionType, uint actionId, long targetedActorId, uint param, uint useType, int pvp, IntPtr a7)
        {
            var ret = _useActionHook!.Original(actionManager, actionType, actionId, targetedActorId, param, useType, pvp, a7);

#if DEBUG
            if (EventManager.Config.LogEvents && EventManager.Config.LogEventCooldownHooks)
            {
                LogDebug("UseActionDetour", $"Result: {ret} Action Type: {actionType} Action ID: {actionId}");
            }
#endif

            try
            {
                if (ret == 1)
                {
                    // Action should be on cooldown now.
                    if (_watchedActions.ContainsKey(actionId))
                    {
                        Update(actionId, actionType);
                    }
                    else if (_actionsModifyingCooldowns.ContainsKey(actionId))
                    {
                        ScheduleUpdate(_actionsModifyingCooldowns[actionId], 25);
                        ScheduleUpdate(_actionsModifyingCooldowns[actionId], 50);
                        ScheduleUpdate(_actionsModifyingCooldowns[actionId], 75);
                        ScheduleUpdate(_actionsModifyingCooldowns[actionId], 400);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "UseActionDetour", $"Error: {ex}");
            }

            return ret;
        }

        private static string? GetSignature<T>(string methodName)
        {
            // https://github.com/CaiClone/GCDTracker/blob/main/src/Data/HelperMethods.cs
            MethodBase? method = typeof(T).GetMethod(methodName);
            if (method == null) { return null; }
            MemberFunctionAttribute attribute = (MemberFunctionAttribute)method.GetCustomAttributes(typeof(MemberFunctionAttribute), true)[0];
            return attribute?.Signature ?? null;
        }
        #endregion

        #region Event Toggle
        public override bool Enable()
        {
            if (base.Enable())
            {
                Plugin.Framework.Update += OnFrameworkUpdate;
                _useActionHook?.Enable();

                return true;
            }

            return false;
        }

        public override bool Disable()
        {
            if (base.Disable())
            {
                Plugin.Framework.Update -= OnFrameworkUpdate;
                _useActionHook?.Disable();
                _delayedUpdates.Clear();
                _cache.Clear();

                return true;
            }

            return false;
        }
        #endregion

        #region Singleton
        private static readonly Lazy<Cooldown> ev = new(() => new Cooldown());
        public static Cooldown Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        public Cooldown()
        {
            _actionManager = ActionManager.Instance();
        }

        protected override void Initialize()
        {
            HookUseAction(); // TOOD: Don't allow enabling if this fails, because it doesn't do shit without the hook.
            base.Initialize();
        }

        protected override void InternalDispose()
        {
            _watchedActions.Clear();
            _actionsModifyingCooldowns.Clear();
            _useActionHook?.Dispose();
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
            else if (x == null)
            {
                return 1;
            }
            else if (y == null)
            {
                return -1;
            }
            else
            {
                return y.Item1.CompareTo(x.Item1);
            }
        }
    }
}
