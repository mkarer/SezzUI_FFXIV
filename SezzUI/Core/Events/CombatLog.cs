using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Network;
using Machina.FFXIV.Headers;
using SezzUI.Helpers;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;

namespace SezzUI.GameEvents
{
	internal sealed class CombatLog : BaseGameEvent
	{
		public event EventHandler? CombatLogEvent;

		#region Singleton

		private static readonly Lazy<CombatLog> _ev = new(() => new());
		public static CombatLog Instance => _ev.Value;
		public static bool Initialized => _ev.IsValueCreated;

		#endregion

		public override bool Enable()
		{
			if (base.Enable())
			{
				Plugin.GameNetwork.NetworkMessage += OnNetworkMessage;
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

		private void OnNetworkMessage(IntPtr data, ushort opcode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
		{
			if (direction == NetworkMessageDirection.ZoneUp)
			{
				return;
			}

			try
			{
				//ParseNetworkMessage(opcode, data - 0x20, sourceActorId, targetActorId);
			}
			catch (Exception ex)
			{
				LogError(ex, "OnNetworkMessage", $"Error: {ex}");
			}
		}

		private static string FixedLength(string input, int length)
		{
			return input.Length > length ? input[..length] : input.PadRight(length, ' ');
		}

		private void ParseNetworkMessage(ushort opCode, IntPtr data, uint sourceActorId, uint targetActorId)
		{
			// https://github.com/ravahn/machina/blob/master/Machina.FFXIV/Headers/Opcodes/Server_MessageType.cs
			switch (opCode)
			{
				case 0x0188:
				{
					// Status Gain/Lost/Effect?
					StatusEffectList packet = Deserialize<StatusEffectList>(data);
					LogDebug("ParseNetworkMessage", ">>> Type: Server_StatusEffectList "
													+ $"JobID {packet.JobID} "
													+ $"Level1 {packet.Level1} " 
													+ $"Level2 {packet.Level2} " 
													+ $"Level3 {packet.Level3} " 
													+ $"HP {packet.CurrentHP}/{packet.MaxHP} " 
													+ $"MP {packet.CurrentMP}/{packet.MaxMP} " 
													+ $"Shield {packet.DamageShield} " 
													+ $"Unknown1 {packet.Unknown1} " 
													+ $"Unknown2 {packet.Unknown2} " 
													+ $"sourceActorId 0x{sourceActorId:X} targetActorId 0x{targetActorId:X} " 
													+ $"ActorID 0x{packet.MessageHeader.ActorID:X}");

					GameObject? statusTargetActor = Plugin.ObjectTable.SearchById(targetActorId);
					string statusTargetName = statusTargetActor?.Name.TextValue ?? "Unknown";
					string jobName = Plugin.DataManager.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>()?.Where(a => a.RowId == packet.JobID).FirstOrDefault()?.NameEnglish.ToString() ?? "";
					int level = Math.Max(Math.Max(packet.Level3, packet.Level2), packet.Level1);
					LogDebug("ParseNetworkMessage", $"Target: {statusTargetName} [0x{targetActorId:X}] (Level {level}{(jobName != "" ? " " + jobName : "")})");

					LogDebug("ParseNetworkMessage", $">>> MessageLength {packet.MessageHeader.MessageLength} MessageType {packet.MessageHeader.MessageType} Unknown1: {packet.MessageHeader.Unknown1} Unknown2: {packet.MessageHeader.Unknown2} Unknown3: {packet.MessageHeader.Unknown3} Unknown4: {packet.MessageHeader.Unknown4}");

					// targetActorId = objectId
					foreach (var eff in packet.Effects)
					{
						//if (eff.ActorID == 0 ) break;

						GameObject? source = Plugin.ObjectTable.SearchById(eff.ActorID);
						string sourceName = source?.Name.TextValue ?? "Unknown";
						string effectName = SpellHelper.GetStatus(eff.EffectID)?.Name.ToDalamudString().ToString() ?? "Unknown Effect";
						
						// duration can be negative?!

						LogDebug("ParseNetworkMessage", $"EffectID {eff.EffectID:0000} [{FixedLength(effectName, 20)}] Duration {eff.Duration:000.00} ActorID 0x{eff.ActorID:X} [{FixedLength(sourceName, 20)}] OtherInfo1 {eff.OtherInfo1} OtherInfo2 {eff.OtherInfo2} ");
					}
				}
					break;
				/*
				case 0x0293:
					ParseCombatLogEvent(Deserialize<Server_StatusEffectList2>(data));
					break;
				case 0x0353:
					ParseCombatLogEvent(Deserialize<Server_StatusEffectList3>(data));
					break;
				case 0x038f:
					ParseCombatLogEvent(Deserialize<Server_BossStatusEffectList>(data));
					break;
				*/
				case 0x033e:
					// {
					// 	// Similar to UNIT_SPELLCAST_SUCCEEDED?
					// 	// Not sure when the other opcodes are used (Server_ActionEffect*) 
					// 	Server_ActionEffect1 packet = Deserialize<Server_ActionEffect1>(data);
					// 	LogDebug("ParseNetworkMessage", "Type: Server_ActionEffect1 " +
					// 									$"Action: {fixedLength(SpellHelper.GetActionName(packet.Header.actionId) ?? "Unknown", 20)} [{fixedLength(packet.Header.actionId.ToString(), 5)}] " +
					// 									$"TargetID: 0x{((IntPtr) packet.TargetID).ToInt64():X} " +
					// 									$"sourceActorId 0x{sourceActorId:X} targetActorId 0x{targetActorId:X} " +
					// 									$"unkn {packet.Header.unknown} displayType {packet.Header.effectDisplayType} SomeTargetID {packet.Header.SomeTargetID} 0x{packet.Header.SomeTargetID:X} vari {packet.Header.variation} unk20 {packet.Header.unknown20}");
					// }
					break;

				/*
				case 0x01f4:
				{
					Server_ActionEffect8 packet = Deserialize<Server_ActionEffect8>(data);
					LogDebug("ParseNetworkMessage", $"Type: Server_ActionEffect8 Action: {SpellHelper.GetActionName(packet.Header.actionId) ?? "Unknown"} [{packet.Header.actionId}]");
				}
					break;
				case 0x01fa:
				{
					Server_ActionEffect16 packet = Deserialize<Server_ActionEffect16>(data);
					LogDebug("ParseNetworkMessage", $"Type: Server_ActionEffect16 Action: {SpellHelper.GetActionName(packet.Header.actionId) ?? "Unknown"} [{packet.Header.actionId}]");
				}
					break;
				case 0x0300:
				{
					Server_ActionEffect24 packet = Deserialize<Server_ActionEffect24>(data);
					LogDebug("ParseNetworkMessage", $"Type: Server_ActionEffect24 Action: {SpellHelper.GetActionName(packet.Header.actionId) ?? "Unknown"} [{packet.Header.actionId}]");
				}
					break;
				case 0x03cd:
				{
					Server_ActionEffect32 packet = Deserialize<Server_ActionEffect32>(data);
					LogDebug("ParseNetworkMessage", $"Type: Server_ActionEffect32 Action: {SpellHelper.GetActionName(packet.Header.actionId) ?? "Unknown"} [{packet.Header.actionId}]");
				}
					break;
				*/
				case 0x0307:
				{
					// NPC casts + Teleport from players + other players casts?
					// Server_ActorCast packet = Deserialize<Server_ActorCast>(data);
					// LogDebug("ParseNetworkMessage", "Type: Server_ActorCast " + $"Action: {fixedLength(SpellHelper.GetActionName(packet.ActionID) ?? "Unknown", 20)} [{fixedLength(packet.ActionID.ToString(), 5)}] " + $"SkillType: {packet.SkillType} " + $"TargetID: 0x{((IntPtr) packet.TargetID).ToInt64():X} " + $"Unk0 {packet.Unknown} " + $"Unk1 {packet.Unknown1} " + $"Unk2 {packet.Unknown2} " + $"Unk3 {packet.Unknown3} " + $"sourceActorId 0x{sourceActorId:X} targetActorId 0x{targetActorId:X} " + $"ActorID 0x{packet.MessageHeader.ActorID:X}");
				}
					break;
				case 0x0203:
				{
					//var packet = Deserialize<Server_EffectResult>(data);
				}
					break;
				case 0x0330:
				{
					//var packet = Deserialize<Server_EffectResultBasic>(data);
				}
					break;
				case 0x02cf:
				{
					// UNRELIABLE
					Server_ActorControl packet = Deserialize<Server_ActorControl>(data);

					// LoseEffect:
					// p1: statusId
					// p3: castSource objectId?

					if (packet.category == Server_ActorControlCategory.HoT_DoT)
					{
						// HoT_DoT:
						// p1: unknown
						// p2: tickInterval?
						// p3: amount?
						// p4: castSource objectId
						GameObject? source = Plugin.ObjectTable.SearchById(packet.param4);
						string sourceName = source?.Name.TextValue ?? "Unknown";
						
						LogDebug("ParseNetworkMessage", $"[SrvACtrl] [{packet.category}] Source: 0x{packet.param4:X} ({FixedLength(sourceName, 10)}) Amount: {packet.param3:D5} Interval: {packet.param2:D3}s Unknown: {packet.param1}");
					}
					else
					{
						//LogDebug("ParseNetworkMessage", $"Type: Server_ActorControl sourceActorId 0x{sourceActorId:X} targetActorId 0x{targetActorId:X} Category {packet.category} p1 {packet.param1} p2 {packet.param2} p3 {packet.param3} p4 {packet.param4}");
					}
				}
					break;

				case 0x0096:
				{
					Server_ActorControlSelf packet = Deserialize<Server_ActorControlSelf>(data);

					// LoseEffect:
					// p1: statusId
					// p3: castSource objectId?

					if (packet.category == Server_ActorControlCategory.HoT_DoT && false)
					{
						// HoT_DoT:
						// p1: unknown
						// p2: tickInterval?
						// p3: amount?
						// p4: castSource objectId
						GameObject? source = Plugin.ObjectTable.SearchById(packet.param4);
						string sourceName = source?.Name.TextValue ?? "Unknown";
						
						LogDebug("ParseNetworkMessage", $"[SrvACtrS] [{packet.category}] Source: 0x{packet.param4:X} ({FixedLength(sourceName, 10)}) Amount: {packet.param3:D5} Interval: {packet.param2:D3}s Unknown: {packet.param1}");
					}
					else
					{
						//LogDebug("ParseNetworkMessage", $"Type: Server_ActorControlSelf sourceActorId 0x{sourceActorId:X} targetActorId 0x{targetActorId:X} Category {packet.category} p1 {packet.param1} p2 {packet.param2} p3 {packet.param3} p4 {packet.param4} p5 {packet.param5} p6 {packet.param6} pad {packet.padding} pad1 {packet.padding1}");
					}
				}
					break;

				case 0x0272:
				{
					// NEVER?
					Server_ActorControlTarget packet = Deserialize<Server_ActorControlTarget>(data);

					// LoseEffect:
					// p1: statusId
					// p3: castSource objectId?

					if (packet.category == Server_ActorControlCategory.HoT_DoT && false)
					{
						// HoT_DoT:
						// p1: unknown
						// p2: tickInterval?
						// p3: amount?
						// p4: castSource objectId
						GameObject? source = Plugin.ObjectTable.SearchById(packet.param4);
						string sourceName = source?.Name.TextValue ?? "Unknown";
						
						LogDebug("ParseNetworkMessage", $"[{packet.category}] Source: 0x{packet.param4:X} ({FixedLength(sourceName, 10)}) Amount: {packet.param3:D5} Interval: {packet.param2:D3}s Unknown: {packet.param1}");
					}
					else
					{
						LogDebug("ParseNetworkMessage", $"[Server_ActorControlTarget] sourceActorId 0x{sourceActorId:X} targetActorId 0x{targetActorId:X} Category {packet.category} p1 {packet.param1} p2 {packet.param2} p3 {packet.param3} p4 {packet.param4} pad {packet.padding} pad1 {packet.padding1} pad2 {packet.padding2} TargetID 0x{packet.TargetID:X} ");
					}
				}

					break;
				
				case 0x00f4:
					//ParseCombatLogEvent(Deserialize<Server_UpdateHpMpTp>(data));
					break;
				case 0x022d:
					//ParseCombatLogEvent(Deserialize<Server_ActorGauge>(data));
					break;
				case 0x0067:
					//ParseCombatLogEvent(Deserialize<Server_PresetWaymark>(data));
					break;
				case 0x00fd:
					//ParseCombatLogEvent(Deserialize<Server_Waymark>(data));
					break;
				case 0x027a:
					//ParseCombatLogEvent(Deserialize<Server_SystemLogMessage>(data));
					break;
				default:
					//throw new NotImplementedException();
					break;
			}
		}

		private static T Deserialize<T>(IntPtr data) where T : struct => Marshal.PtrToStructure<T>(data);
		
		public static unsafe byte[] Serialize<T>(T value) where T : unmanaged
		{
			byte[] buffer = new byte[sizeof(T)];

			fixed (byte* bufferPtr = buffer)
			{
				Buffer.MemoryCopy(&value, bufferPtr, sizeof(T), sizeof(T));
			}

			return buffer;
		}
		
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct StatusEffectListEntry
		{
			public ushort EffectID;
			public byte OtherInfo1;
			public byte OtherInfo2;
			public float Duration;
			public uint ActorID;
		}
		
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct StatusEffectList
		{
			public Server_MessageHeader MessageHeader;
			public byte JobID;
			public byte Level1;
			public byte Level2;
			public byte Level3;
			public uint CurrentHP;
			public uint MaxHP;
			public ushort CurrentMP;
			public ushort MaxMP;
			public byte DamageShield;
			public ushort Unknown1; // used to be TP
			public byte Unknown2;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 30 * 3 * 4)] public StatusEffectListEntry[] Effects;
		}
	}
}