using System;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SezzUI.Hooking
{
	internal class OriginalFunctionManager : IDisposable
	{
		#region GetAdjustedActionId

		private delegate uint GetAdjustedActionIdDelegate(IntPtr actionManager, uint actionId);

		private static OriginalFunction<GetAdjustedActionIdDelegate>? _originalGetAdjustedActionId;
		private static bool _triedUnhookingGetAdjustedActionId;

		public static unsafe uint GetAdjustedActionId(uint actionId)
		{
			if (!_triedUnhookingGetAdjustedActionId)
			{
				_triedUnhookingGetAdjustedActionId = true;

				try
				{
					// Client::Game::ActionManager.GetAdjustedActionId
					string getAdjustedActionIdSig = Helpers.AsmHelper.GetSignature<ActionManager>("GetAdjustedActionId") ?? "E8 ?? ?? ?? ?? 8B F8 3B DF";
					_originalGetAdjustedActionId = new(getAdjustedActionIdSig, "81 FA 2D 01 00 00 7F 42 0F 84 4B 01 00 00 8D 42 EB");
				}
				catch (Exception ex)
				{
					Plugin.ChatGui.PrintError("SezzUI failed to reassemble GetAdjustedActionId to work around hooking issues.");
					Plugin.ChatGui.PrintError("If you're using XIVCombo or a similar plugin most action-related features won't work correctly.");
					Plugin.ChatGui.PrintError("You can (and should) check the Dalamud logfile for further details.");
					PluginLog.Error(ex, ex.Message);
				}
			}

			ActionManager* actionManager = ActionManager.Instance();
			return _originalGetAdjustedActionId?.Invoke?.Invoke((IntPtr) actionManager, actionId) ?? actionManager->GetAdjustedActionId(actionId);
		}

		#endregion

		#region Singleton

		public static void Initialize()
		{
			Instance = new();
		}

		public OriginalFunctionManager()
		{
			_triedUnhookingGetAdjustedActionId = false;
		}

		public static OriginalFunctionManager Instance { get; private set; } = null!;

		~OriginalFunctionManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_originalGetAdjustedActionId?.Dispose();

			Instance = null!;
		}

		#endregion
	}
}