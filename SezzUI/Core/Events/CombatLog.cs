using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Network;
using Machina.FFXIV.Headers;
using SezzUI.Helpers;

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
				ParseNetworkMessage(opcode, data - 0x20);
			}
			catch (Exception ex)
			{
				LogError(ex, "OnNetworkMessage", $"Error: {ex}");
			}
		}

		private void ParseNetworkMessage(ushort opCode, IntPtr data)
		{
			switch (opCode)
			{
				/*
				case 0x0188:
					ParseCombatLogEvent(Deserialize<Server_StatusEffectList>(data));
					break;
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
				{
					// UNIT_SPELLCAST_SUCCEEDED
					Server_ActionEffect1 packet = Deserialize<Server_ActionEffect1>(data);
					LogDebug("ParseNetworkMessage", $"Type: Server_ActionEffect1 Action: {SpellHelper.GetActionName(packet.Header.actionId) ?? "Unknown"} [{packet.Header.actionId}]");
				}
					break;

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
				/*
				case 0x0307:
					ParseCombatLogEvent(Deserialize<Server_ActorCast>(data));
					break;
				case 0x0203:
					ParseCombatLogEvent(Deserialize<Server_EffectResult>(data));
					break;
				case 0x0330:
					ParseCombatLogEvent(Deserialize<Server_EffectResultBasic>(data));
					break;
				case 0x02cf:
					ParseCombatLogEvent(Deserialize<Server_ActorControl>(data));
					break;
				case 0x0096:
					ParseCombatLogEvent(Deserialize<Server_ActorControlSelf>(data));
					break;
				case 0x0272:
					ParseCombatLogEvent(Deserialize<Server_ActorControlTarget>(data));
					break;
				case 0x00f4:
					ParseCombatLogEvent(Deserialize<Server_UpdateHpMpTp>(data));
					break;
				case 0x022d:
					ParseCombatLogEvent(Deserialize<Server_ActorGauge>(data));
					break;
				case 0x0067:
					ParseCombatLogEvent(Deserialize<Server_PresetWaymark>(data));
					break;
				case 0x00fd:
					ParseCombatLogEvent(Deserialize<Server_Waymark>(data));
					break;
				case 0x027a:
					ParseCombatLogEvent(Deserialize<Server_SystemLogMessage>(data));
					break;
				default:
					throw new NotImplementedException();
			*/
			}
		}

		private static T Deserialize<T>(IntPtr data) where T : struct => Marshal.PtrToStructure<T>(data);
	}
}