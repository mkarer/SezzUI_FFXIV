using System;
using System.IO;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Network;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace SezzUI.GameEvents
{
	internal sealed class DutyFinderQueue : BaseGameEvent
	{
		private const ushort HEADER_SIZE = 0x20;

		public delegate void JoinedDelegate();

		public event JoinedDelegate? Joined;

		public delegate void UpdateDelegate(byte queuePosition, byte waitTime, uint contentFinderConditionId);

		public event UpdateDelegate? Update;

		public delegate void LeftDelegate();

		public event LeftDelegate? Left;

		public delegate void ReadyDelegate();

		public event ReadyDelegate? Ready;

		public byte Position { get; private set; }
		public byte EstimatedWaitTime { get; private set; }

		public bool IsQueued => Plugin.Condition[ConditionFlag.BoundToDuty97];

		public override bool Enable()
		{
			if (base.Enable())
			{
				Plugin.GameNetwork.NetworkMessage += OnNetworkMessage;

				if (IsQueued)
				{
					InvokeJoined();
				}

				return true;
			}

			return false;
		}

		public override bool Disable()
		{
			if (base.Disable())
			{
				Plugin.GameNetwork.NetworkMessage -= OnNetworkMessage;
				return true;
			}

			return false;
		}

		private unsafe UnmanagedMemoryStream? GetDataStream(IntPtr dataPtr)
		{
			uint dataSize = 0;
			IntPtr headerPtr = dataPtr - HEADER_SIZE;

			try
			{
				using (UnmanagedMemoryStream stream = new((byte*) headerPtr.ToPointer(), HEADER_SIZE))
				{
					using (BinaryReader reader = new(stream))
					{
						try
						{
							dataSize = reader.ReadUInt32();
						}
						catch (Exception ex)
						{
							Logger.Error(ex, "GetDataStream", $"Error: {ex}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "GetDataStream", $"Error: {ex}");
			}

			if (dataSize != 0)
			{
				return new((byte*) dataPtr.ToPointer(), dataSize - HEADER_SIZE);
			}

			return null;
		}

		private void InvokeJoined()
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug("Signed up for duty.");
			}
#endif
			Joined?.Invoke();
		}

		private void InvokeLeft()
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug("Left duty finder queue.");
			}
#endif
			Left?.Invoke();
		}

		private void InvokeReady()
		{
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug("Duty finder queue is ready!");
			}
#endif
			Ready?.Invoke();
		}

		private void InvokeUpdate(byte queuePosition, byte waitTime, uint contentFinderConditionId)
		{
			Position = queuePosition;
			EstimatedWaitTime = waitTime;

			string? dutyName = null;
			ExcelSheet<ContentFinderCondition>? cfConditionSheet = Plugin.DataManager.GetExcelSheet<ContentFinderCondition>();
			if (cfConditionSheet != null)
			{
				ContentFinderCondition? cfCondition = cfConditionSheet.GetRow(contentFinderConditionId);
				if (cfCondition == null)
				{
					Logger.Error($"Error: Unknown ContentFinderCondition ID: {contentFinderConditionId}");
				}
				else
				{
					dutyName = cfCondition.Name.ToString();
					if (dutyName.IsNullOrEmpty())
					{
						dutyName = "Duty Roulette";
					}
				}
			}

#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventDutyFinderQueue)
			{
				Logger.Debug($"Status update: Position {queuePosition} Estimated Wait Time: {waitTime} ContentFinderCondition ID: {contentFinderConditionId} Duty: {dutyName ?? "Unknown"}");
			}
#endif

			Update?.Invoke(queuePosition, waitTime, contentFinderConditionId);
		}

		private void OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
		{
			if (direction == NetworkMessageDirection.ZoneUp)
			{
				return;
			}

			switch (opCode)
			{
				case 0x188:
				{
					// Status Update
					UnmanagedMemoryStream? stream = GetDataStream(dataPtr);
					if (stream == null)
					{
						return;
					}

					using BinaryReader reader = new(stream);
					try
					{
						reader.BaseStream.Position = 0x24 - HEADER_SIZE;
						ushort contentFinderConditionId = reader.ReadUInt16();

						reader.BaseStream.Position = 0x28 - HEADER_SIZE;
						byte queuePosition = reader.ReadByte();

						reader.BaseStream.Position = 0x29 - HEADER_SIZE;
						byte waitTime = reader.ReadByte(); // The game displays 31 as "More than 30m" and <5 as "Less than 5m"

						InvokeUpdate(queuePosition, waitTime, contentFinderConditionId);
					}
					catch (Exception ex)
					{
						Logger.Error(ex, "OnNetworkMessage", $"Error while parsing (opCode: {opCode}): {ex}");
					}

					break;
				}

				case 0xa9:
					// Joined
					InvokeJoined();
					break;

				case 0xeb:
					// Registration withdrawn
					InvokeLeft();
					break;

				case 0x1c5:
					// Ready
					InvokeReady();
					break;
			}
		}

		#region Singleton

		private static readonly Lazy<DutyFinderQueue> _ev = new(() => new());
		public static DutyFinderQueue Instance => _ev.Value;
		public static bool Initialized => _ev.IsValueCreated;

		#endregion
	}
}