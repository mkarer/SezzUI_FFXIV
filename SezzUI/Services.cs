using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SezzUI;

internal class Services
{
	[PluginService]
	internal static IBuddyList BuddyList { get; private set; } = null!;

	[PluginService]
	internal static IChatGui Chat { get; private set; } = null!;

	[PluginService]
	internal static IClientState ClientState { get; private set; } = null!;

	[PluginService]
	internal static ICommandManager Commands { get; private set; } = null!;

	[PluginService]
	internal static ICondition Condition { get; private set; } = null!;

	[PluginService]
	internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

	[PluginService]
	internal static IDataManager Data { get; private set; } = null!;

	[PluginService]
	internal static IDtrBar DtrBar { get; private set; } = null!;

	[PluginService]
	internal static IFramework Framework { get; private set; } = null!;

	[PluginService]
	internal static IGameGui GameGui { get; private set; } = null!;

	[PluginService]
	internal static IJobGauges JobGauges { get; private set; } = null!;

	[PluginService]
	internal static IObjectTable Objects { get; private set; } = null!;

	[PluginService]
	internal static ISigScanner SigScanner { get; private set; } = null!;

	[PluginService]
	internal static ITargetManager TargetManager { get; private set; } = null!;

	[PluginService]
	internal static IGameInteropProvider HookProvider { get; private set; } = null!;

	[PluginService]
	internal static IPluginLog PluginLog { get; private set; } = null!;

	[PluginService]
	internal static ITextureProvider TextureProvider { get; private set; } = null!;
}