using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace SezzUI
{
	public class Service
	{
		[PluginService]
		internal static BuddyList BuddyList { get; private set; } = null!;

		[PluginService]
		internal static ChatGui ChatGui { get; private set; } = null!;

		[PluginService]
		internal static ClientState ClientState { get; private set; } = null!;

		[PluginService]
		internal static CommandManager CommandManager { get; private set; } = null!;

		[PluginService]
		internal static Condition Condition { get; private set; } = null!;

		[PluginService]
		internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

		[PluginService]
		internal static DataManager DataManager { get; private set; } = null!;

		[PluginService]
		internal static DtrBar DtrBar { get; private set; } = null!;

		[PluginService]
		internal static Framework Framework { get; private set; } = null!;

		[PluginService]
		internal static GameGui GameGui { get; private set; } = null!;

		[PluginService]
		internal static JobGauges JobGauges { get; private set; } = null!;

		[PluginService]
		internal static ObjectTable ObjectTable { get; private set; } = null!;

		[PluginService]
		internal static SigScanner SigScanner { get; private set; } = null!;

		[PluginService]
		internal static TargetManager TargetManager { get; private set; } = null!;
	}
}