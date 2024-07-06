using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Win32;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Plugin.Services;
using SezzUI.Configuration;
using SezzUI.Interface.GeneralElements;
using SezzUI.Logging;

namespace SezzUI.Helper;

public sealed class NaturalStringComparer : IComparer<string>
{
	public int Compare(string? x, string? y) => PInvoke.StrCmpLogical(x ?? "", y ?? "");
}

internal static class Utils
{
	internal static PluginLogger Logger;

	public static NaturalStringComparer NaturalStringComparer = new();

	static Utils()
	{
		Logger = new("Utils");
	}

	public static unsafe bool IsHostileMemory(IBattleNpc? npc) => npc != null && (npc.BattleNpcKind == BattleNpcSubKind.Enemy || (int) npc.BattleNpcKind == 1) && *(byte*) (npc.Address + 0x19C3) != 0;

	public static PluginConfigColor ColorForActor(IGameObject? actor)
	{
		if (actor == null || actor is not ICharacter character)
		{
			return Singletons.Get<GlobalColors>().NPCNeutralColor;
		}

		if (character.ObjectKind == ObjectKind.Player)
		{
			return Singletons.Get<GlobalColors>().SafeColorForJobId(character.ClassJob.Id);
		}

		return character switch
		{
			IBattleNpc {SubKind: 9} battleNpc when battleNpc.ClassJob.Id > 0 => Singletons.Get<GlobalColors>().SafeColorForJobId(character.ClassJob.Id), // Trust/Squadron NPCs
			IBattleNpc battleNpc when battleNpc.BattleNpcKind is BattleNpcSubKind.Chocobo or BattleNpcSubKind.Pet || !IsHostileMemory(battleNpc) => Singletons.Get<GlobalColors>().NPCFriendlyColor,
			IBattleNpc battleNpc when battleNpc.BattleNpcKind == BattleNpcSubKind.Enemy || (battleNpc.StatusFlags & StatusFlags.InCombat) == StatusFlags.InCombat => Singletons.Get<GlobalColors>().NPCHostileColor, // I still don't think we should be defaulting to "in combat = hostile", but whatever
			_ => Singletons.Get<GlobalColors>().NPCNeutralColor
		};
	}

	public static Status? GetTankInvulnerabilityID(IBattleChara actor)
	{
		Status? tankInvulnBuff = actor.StatusList.FirstOrDefault(o => o.StatusId is 810 or 811 or 1302 or 409 or 1836 or 82);

		return tankInvulnBuff;
	}

	public static bool IsOnCleanseJob()
	{
		IPlayerCharacter? player = Services.ClientState.LocalPlayer;

		return player != null && JobsHelper.IsJobWithCleanse(player.ClassJob.Id, player.Level);
	}

	public static IGameObject? FindTargetOfTarget(IGameObject? target, IGameObject? player, IObjectTable actors)
	{
		if (target == null)
		{
			return null;
		}

		if (target.TargetObjectId == 0 && player != null && player.TargetObjectId == 0)
		{
			return player;
		}

		// only the first 200 elements in the array are relevant due to the order in which SE packs data into the array
		// we do a step of 2 because its always an actor followed by its companion
		for (int i = 0; i < 200; i += 2)
		{
			IGameObject? actor = actors[i];
			if (actor?.GameObjectId == target.TargetObjectId)
			{
				return actor;
			}
		}

		return null;
	}

	public static string UserFriendlyConfigName(string configTypeName) => UserFriendlyString(configTypeName, "Config");

	public static string UserFriendlyString(string str, string? remove)
	{
		string? s = remove != null ? str.Replace(remove, "") : str;

		Regex? regex = new(@"
                    (?<=[A-Z])(?=[A-Z][a-z]) |
                    (?<=[^A-Z])(?=[A-Z]) |
                    (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

		return regex.Replace(s, " ");
	}

	public static void OpenFolder(string path)
	{
		path = $"\"{path}\"";

		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Process.Start("explorer", path);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process.Start("mimeopen", path);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				Process.Start("open", $"-R {path}");
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Error trying to open folder: {ex}");
		}
	}

	public static void OpenUrl(string url)
	{
		try
		{
			Process.Start(url);
		}
		catch
		{
			try
			{
				// hack because of this: https://github.com/dotnet/corefx/issues/10361
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					Process.Start("xdg-open", url);
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Error trying to open URL: {ex}");
			}
		}
	}
}