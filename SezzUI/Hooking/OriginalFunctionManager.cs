using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using SezzUI.Logging;
using SezzUI.Modules;

namespace SezzUI.Hooking;

internal class OriginalFunctionManager : IPluginDisposable
{
	internal static PluginLogger Logger = null!;

	public OriginalFunctionManager()
	{
		Logger = new("OriginalFunctionManager");
		_triedUnhookingGetAdjustedActionId = false;
	}

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
				// throw new("Currently unsupported.");
				// TODO: https://github.com/goatcorp/Dalamud/pull/1843
				_originalGetAdjustedActionId = new("E8 ?? ?? ?? ?? 89 03 8B 03", "48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 8B DA 81 FA 65 A9 00 00 0F 8F C7 03 00 00");
			}
			catch (Exception ex)
			{
				Services.Chat.PrintError("SezzUI failed to reassemble GetAdjustedActionId to work around hooking issues.");
				Services.Chat.PrintError("If you're using XIVCombo or a similar plugin most action-related features won't work correctly.");
				Services.Chat.PrintError("You can (and should) check the Dalamud logfile for further details.");
				Logger.Error(ex);
			}
		}

		ActionManager* actionManager = ActionManager.Instance();
		return _originalGetAdjustedActionId?.Invoke?.Invoke((IntPtr) actionManager, actionId) ?? actionManager->GetAdjustedActionId(actionId);
	}

	#endregion

	bool IPluginDisposable.IsDisposed { get; set; } = false;

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
		if (!disposing || (this as IPluginDisposable).IsDisposed)
		{
			return;
		}

		_originalGetAdjustedActionId?.Dispose();

		(this as IPluginDisposable).IsDisposed = true;
	}
}