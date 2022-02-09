using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using SezzUI.GameStructs;

namespace SezzUI.GameEvents
{
	internal sealed class DutyFinderQueue : BaseGameEvent
	{
		public static unsafe ContentsFinderQueue* Queue => (ContentsFinderQueue*) _queue;
		private static IntPtr _queue;
		private static DateTime? _queueStarted;
		public unsafe byte Position => Queue != null && Queue->QueuePosition != byte.MaxValue ? Queue->QueuePosition : (byte) 0;
		public unsafe byte AverageWaitTime => Queue != null ? Queue->AverageWaitTime : (byte) 0;
		public byte EstimatedWaitTime => _queueStarted != null && AverageWaitTime > 0 ? (byte) Math.Clamp(Math.Ceiling((((DateTime) _queueStarted).AddMinutes(AverageWaitTime) - DateTime.Now).TotalMinutes), 0, byte.MaxValue) : (byte) 0;
		public unsafe bool IsQueued => Queue != null ? Queue->IsQueued() : false;
		public unsafe bool IsQueueing => Queue != null ? Queue->IsQueueing() : false;
		public unsafe bool IsReady => Queue != null ? Queue->IsReady() : false;
		public unsafe byte ContentRouletteId => Queue != null ? Queue->ContentRouletteId : (byte) 0;
		public unsafe uint ContentFinderConditionId => Queue != null ? Queue->ContentFinderConditionId : 0u;
		public ContentFinderCondition? ContentFinderCondition => ContentFinderConditionId != 0 ? Plugin.DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow(ContentFinderConditionId) : null;
		public ContentRoulette? ContentRoulette => ContentRouletteId != 0 ? Plugin.DataManager.GetExcelSheet<ContentRoulette>()?.GetRow(ContentRouletteId) : null;

		private delegate IntPtr JoinQueueDelegate(IntPtr queue, int unused, byte unk1);

		[Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B FA 48 8B D9 45 84 C0", DetourName = nameof(JoinQueueDetour))] // ReSharper disable once UnusedAutoPropertyAccessor.Local, 6.0.8: 0x1407AC4A0
		private Hook<JoinQueueDelegate>? JoinQueueHook { get; init; }

		private delegate void UpdateQueueStateDelegate(IntPtr queue, byte state);

		[Signature("40 53 48 83 EC 20 0F B6 59 55", DetourName = nameof(UpdateQueueStateDetour))] // ReSharper disable once UnusedAutoPropertyAccessor.Local, 6.0.8: 1407AD4E0
		private Hook<UpdateQueueStateDelegate>? UpdateQueueStateHook { get; init; }

		private delegate IntPtr OnQueueLeftDelegate(IntPtr queue, uint unk0, IntPtr objectId);

		[Signature("4C 8B DC 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? C6 01 00", DetourName = nameof(OnQueueLeftDetour))] // ReSharper disable once UnusedAutoPropertyAccessor.Local, 6.0.8: 0x1407AC580
		private Hook<OnQueueLeftDelegate>? OnQueueLeftHook { get; init; }

		public delegate void DutyFinderEventDelegate();

		public event DutyFinderEventDelegate? Joined;

		public event DutyFinderEventDelegate? Left;

		public event DutyFinderEventDelegate? Ready;

		public event DutyFinderEventDelegate? Update;

		public override bool Enable()
		{
			if (!base.Enable())
			{
				return false;
			}

			JoinQueueHook?.Enable();
			OnQueueLeftHook?.Enable();
			UpdateQueueStateHook?.Enable();
			Plugin.ClientState.Logout += OnLogout; // Shouldn't be needed, better be safe though.

			if (IsQueued)
			{
				InvokeJoined();
			}

			return true;
		}

		public override bool Disable()
		{
			if (!base.Disable())
			{
				return false;
			}

			JoinQueueHook?.Disable();
			OnQueueLeftHook?.Disable();
			UpdateQueueStateHook?.Disable();
			Plugin.ClientState.Logout -= OnLogout;
			Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
			_queueStarted = null;

			return true;
		}

		private void OnLogout(object? sender, EventArgs e)
		{
			InvokeLeft();
		}

		private unsafe void OnTerritoryChanged(object? sender, ushort territoryType)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug("OnTerritoryChanged", $"territoryType: {territoryType} ContentFinderCondition.TerritoryType {ContentFinderCondition?.TerritoryType} QueueState2: {(Queue != null ? Queue->QueueState2 : 0)} QueueState3: {(Queue != null ? Queue->QueueState3 : 0)} Position: {Position} AverageWaitTime: {AverageWaitTime} EstimatedWaitTime: {EstimatedWaitTime} ContentFinderConditionId: {Queue->ContentFinderConditionId} ContentRouletteId: {Queue->ContentRouletteId} WaitingForDuty {Plugin.Condition[ConditionFlag.WaitingForDuty]} BoundByDuty {Plugin.Condition[ConditionFlag.BoundByDuty]} BoundByDuty56 {Plugin.Condition[ConditionFlag.BoundByDuty56]} BoundByDuty95 {Plugin.Condition[ConditionFlag.BoundByDuty95]}");
			}
#endif

			if (_queueStarted != null && Queue != null && ((ContentFinderCondition?.TerritoryType.Row ?? ushort.MaxValue) == territoryType || Queue->IsInDuty() || Plugin.Condition[ConditionFlag.BoundByDuty] || Plugin.Condition[ConditionFlag.BoundByDuty56] || Plugin.Condition[ConditionFlag.BoundByDuty95] || Plugin.Condition[ConditionFlag.WaitingForDuty]))
			{
				InvokeLeft(); // In a duty, not in the queue.
			}
		}

		private void InvokeJoined()
		{
			if (_queueStarted != null)
			{
				return;
			}

#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug("Signed up for duty.");
			}
#endif
			_queueStarted = DateTime.Now;
			Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;

			try
			{
				Joined?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "InvokeJoined", $"Failed invoking {nameof(Joined)}: {ex}");
			}
		}

		private void InvokeLeft()
		{
			Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;

			if (_queueStarted == null)
			{
				return;
			}

			_queueStarted = null;

#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug("Left duty finder queue (or joined an instance).");
			}
#endif

			try
			{
				Left?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "InvokeLeft", $"Failed invoking {nameof(Left)}: {ex}");
			}
		}

		private void InvokeReady()
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug("Duty finder queue is ready!");
			}
#endif

			try
			{
				Ready?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "InvokeReady", $"Failed invoking {nameof(Ready)}: {ex}");
			}
		}


		private unsafe void InvokeUpdate()
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				string dutyName = ContentFinderConditionId != 0 ? ContentFinderCondition?.Name ?? "Unknown" : ContentRouletteId != 0 ? ContentRoulette?.Name ?? "Unknown" : "Unknown";
				Logger.Debug("InvokeUpdate", $"QueueState2: {(Queue != null ? Queue->QueueState2 : 0)} QueueState3: {(Queue != null ? Queue->QueueState3 : 0)} Position: {Position} AverageWaitTime: {AverageWaitTime} EstimatedWaitTime: {EstimatedWaitTime} ContentFinderConditionId: {Queue->ContentFinderConditionId} ContentRouletteId: {Queue->ContentRouletteId} Duty: {dutyName}");
			}
#endif

			try
			{
				Update?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "InvokeUpdate", $"Failed invoking {nameof(Update)}: {ex}");
			}
		}

		private IntPtr JoinQueueDetour(IntPtr queue, int unused, byte unk1)
		{
			IntPtr output = JoinQueueHook!.Original(queue, unused, unk1);

#if DEBUG
			LogQueueDetails("JoinQueueDetour");
#endif
			InvokeJoined();

			return output;
		}

		private IntPtr OnQueueLeftDetour(IntPtr queue, uint unk0, IntPtr objectId)
		{
			IntPtr output = OnQueueLeftHook!.Original(queue, unk0, objectId);

#if DEBUG
			LogQueueDetails("OnQueueLeftDetour");
#endif
			InvokeLeft();

			return output;
		}

		private void UpdateQueueStateDetour(IntPtr queue, byte state)
		{
			UpdateQueueStateHook!.Original(queue, state);

#if DEBUG
			LogQueueDetails("UpdateQueueStateDetour");
#endif

			if (IsReady)
			{
				InvokeReady();
			}
			else
			{
				InvokeUpdate();
			}
		}

#if DEBUG
		private unsafe void LogQueueDetails(string messagePrefix)
		{
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				string dutyName = ContentFinderConditionId != 0 ? ContentFinderCondition?.Name ?? "Unknown" : ContentRouletteId != 0 ? ContentRoulette?.Name ?? "Unknown" : "Unknown";
				Logger.Debug(messagePrefix, $"QueueState2: {(Queue != null ? Queue->QueueState2 : 0)} QueueState3: {(Queue != null ? Queue->QueueState3 : 0)} Position: {Position} AverageWaitTime: {AverageWaitTime} EstimatedWaitTime: {EstimatedWaitTime} ContentFinderConditionId: {Queue->ContentFinderConditionId} ContentRouletteId: {Queue->ContentRouletteId} Duty: {dutyName}");
			}
		}
#endif

		#region Singleton

		private static readonly Lazy<DutyFinderQueue> _ev = new(() => new());

		public static DutyFinderQueue Instance => _ev.Value;
		public static bool Initialized => _ev.IsValueCreated;

		protected override unsafe void Initialize()
		{
			SignatureHelper.Initialise(this);
			_queue = (IntPtr) UIState.Instance() + 0x11978;
			base.Initialize();
		}

		protected override void InternalDispose()
		{
			JoinQueueHook?.Dispose();
			OnQueueLeftHook?.Dispose();
			UpdateQueueStateHook?.Dispose();
		}

		#endregion
	}
}

namespace SezzUI.GameStructs
{
	[StructLayout(LayoutKind.Explicit, Size = 0xB0)]
	public struct ContentsFinderQueue
	{
		[FieldOffset(0x75)]
		public byte QueueState2; // 0 -> 1 -> 2 (Queued), 3 (Ready), 5 (In Duty)

		[FieldOffset(0x76)]
		public byte QueueState3; // 4 (Always 4 when doing anything related to duties?)

		[FieldOffset(0x7A)]
		public byte ContentRouletteId;

		[FieldOffset(0x82)]
		public ushort ContentFinderConditionId;

		[FieldOffset(0x86)]
		public byte QueuePosition;

		[FieldOffset(0x87)]
		public byte AverageWaitTime;

		/// <summary>
		///     Queued and waiting for the queue to pop or waiting for everyone to accept.
		/// </summary>
		/// <returns></returns>
		public bool IsQueued() => IsReady() || QueueState2 == 2;

		/// <summary>
		///     Queue request sent but not yet acknowledged.
		/// </summary>
		/// <returns></returns>
		public bool IsQueueing() => QueueState2 < 2 && QueueState3 == 4;

		/// <summary>
		///     Ready to enter duty.
		/// </summary>
		/// <returns></returns>
		public bool IsReady() => QueueState2 == 3 && QueueState3 == 4;

		/// <summary>
		///     In duty.
		/// </summary>
		/// <returns></returns>
		public bool IsInDuty() => QueueState2 == 5 && QueueState3 == 4;
	}
}