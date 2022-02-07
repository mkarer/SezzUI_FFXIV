using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets;
using SezzUI.GameStructs;

namespace SezzUI.GameEvents
{
	internal sealed class DutyFinderQueue : BaseGameEvent
	{
		public static unsafe ContentsFinderQueue* Queue => (ContentsFinderQueue*) _queuePointer;
		private static IntPtr _queuePointer = IntPtr.Zero;
		private static DateTime? _queueStarted;

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		private delegate void ProcessZonePacketDownDelegate(IntPtr a, uint targetId, IntPtr dataPtr);

		[Signature("48 89 5C 24 ?? 56 48 83 EC 50 8B F2", DetourName = nameof(ProcessZonePacketDownDetour))] // ReSharper disable once UnusedAutoPropertyAccessor.Local
		private Hook<ProcessZonePacketDownDelegate>? ProcessZonePacketDownHook { get; init; }

		public unsafe byte Position => Queue != null && Queue->QueuePosition != byte.MaxValue ? Queue->QueuePosition : (byte) 0;
		public unsafe byte AverageWaitTime => Queue != null ? Queue->AverageWaitTime : (byte) 0;
		public byte EstimatedWaitTime => _queueStarted != null && AverageWaitTime > 0 ? (byte) Math.Clamp(Math.Ceiling((((DateTime) _queueStarted).AddMinutes(AverageWaitTime) - DateTime.Now).TotalMinutes), 0, byte.MaxValue) : (byte) 0;
		public unsafe bool IsQueued => Queue != null ? Queue->IsQueued() : false;
		public unsafe bool IsQueueing => Queue != null ? Queue->IsQueueing() : false;
		public unsafe bool IsReady => Queue != null ? Queue->IsReady() : false;
		public unsafe byte ContentRouletteId => Queue != null ? Queue->ContentRouletteId : (byte) 0;
		public unsafe uint ContentFinderConditionId => Queue != null ? Queue->ContentFinderConditionId : 0u;

		private unsafe void FindQueueAddress()
		{
			// .text:000000000081E9AC	_sub_81E980_AgentContentsFinderRelated	lea     rcx, _unk_1EBE178_AgentContentsFinderStuff
			//                                                                  lea     rcx, [7FF7DD1AE178h]
			if (Plugin.SigScanner.TryScanText("48 8D 0D ?? ?? ?? ?? 4D 8B E0 4C 8B EA", out IntPtr result))
			{
				uint offset = BinaryPrimitives.ReadUInt32LittleEndian(new((byte*) result + 3, 4));
				IntPtr data = new(result.ToInt64() + offset + 7);

#if DEBUG
				if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
				{
					Logger.Debug("FindQueueAddress", $"Struct Instruction Signature: 0x{result:X}");
					Logger.Debug("FindQueueAddress", $"Struct Memory Offset: 0x{offset:X}");
					Logger.Debug("FindQueueAddress", $"Struct Data Pointer: 0x{data:X}");
				}
#endif
				_queuePointer = data;
			}
			else
			{
				Plugin.ChatGui.PrintError("SezzUI failed to find the duty finder queue address, queue data won't be available.");
				Logger.Error("FindQueueAddress", "Signature not found!");
			}
		}

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

			ProcessZonePacketDownHook?.Enable();
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

			ProcessZonePacketDownHook?.Disable();
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

		public ContentFinderCondition? ContentFinderCondition => ContentFinderConditionId != 0 ? Plugin.DataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow(ContentFinderConditionId) : null;
		public ContentRoulette? ContentRoulette => ContentRouletteId != 0 ? Plugin.DataManager.GetExcelSheet<ContentRoulette>()?.GetRow(ContentRouletteId) : null;

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

		private void ProcessZonePacketDownDetour(IntPtr a, uint targetId, IntPtr dataPtr)
		{
			// TODO: Hook the function that updates the queue position, I couldn't find a signature.
			try
			{
				ProcessZonePacketDownHook!.Original(a, targetId, dataPtr);

				ushort opCode = (ushort) Marshal.ReadInt16(dataPtr, 0x2);
				switch (opCode)
				{
					case 0x188: // Status Update
						InvokeUpdate();
						break;

					case 0xa9: // Queue joined
						InvokeJoined();
						break;

					case 0xeb: // Registration withdrawn
						InvokeLeft();
						break;

					case 0x1c5: // Queue ready
						InvokeReady();
						break;
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "ProcessZonePacketDownDetour", $"Error: {ex}");
			}
		}

		#region Singleton

		private static readonly Lazy<DutyFinderQueue> _ev = new(() => new());

		public static DutyFinderQueue Instance => _ev.Value;
		public static bool Initialized => _ev.IsValueCreated;

		protected override void Initialize()
		{
			SignatureHelper.Initialise(this);
			FindQueueAddress();
			base.Initialize();
		}

		protected override void InternalDispose()
		{
			ProcessZonePacketDownHook?.Dispose();
		}

		#endregion
	}
}

namespace SezzUI.GameStructs
{
	[StructLayout(LayoutKind.Explicit, Size = 0x100)]
	public struct ContentsFinderQueue
	{
		// [FieldOffset(0x40)]
		// public byte QueueState1;

		[FieldOffset(0x75)]
		public byte QueueState2; // 0 -> 1 -> 2 (Queued), 3 (Ready), 5 (In Duty)

		[FieldOffset(0x76)]
		public byte QueueState3; // 4 (Always 4 when doing anything related to duties?)

		[FieldOffset(0x7A)]
		public byte ContentRouletteId;

		// [FieldOffset(0x7B)]
		// public byte QueuePosition1;

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