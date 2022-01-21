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
					_originalGetAdjustedActionId = new("E8 ?? ?? ?? ?? 8B F8 3B DF", "81 FA 2D 01 00 00 7F 42 0F 84 4B 01 00 00 8D 42 EB");
				}
				catch (Exception ex)
				{
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