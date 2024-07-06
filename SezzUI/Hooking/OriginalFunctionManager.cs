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
				throw new("Currently unsupported.");
				// TODO: https://github.com/goatcorp/Dalamud/pull/1843
				// string getAdjustedActionIdSig = AsmHelper.GetSignature<ActionManager>("GetAdjustedActionId") ?? "E8 ?? ?? ?? ?? 89 03 8B 03";
				// _originalGetAdjustedActionId = new(getAdjustedActionIdSig, "81 FA 2D 01 00 00 7F 42 0F 84 4B 01 00 00 8D 42 EB"); // TODO: Needs to get updated first.
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