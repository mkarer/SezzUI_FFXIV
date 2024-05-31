using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Hooking;
using SezzUI.Logging;
using SezzUI.Modules;

namespace SezzUI.Hooking;

public interface IHookAccessor
{
	List<IHookWrapper>? Hooks { get; protected set; }

	void EnableHooks()
	{
		if (!Hooks?.Any() ?? true)
		{
			return;
		}

#if DEBUG
		(this as IPluginLogger)?.Logger.Debug($"Enabling {Hooks?.Where(hook => !hook.IsEnabled && !hook.IsDisposed).Count() ?? 0}/{Hooks?.Count ?? 0} hook(s)");
#endif

		Hooks?.ForEach(hook => hook.Enable());
	}

	void DisableHooks()
	{
		if (!Hooks?.Any() ?? true)
		{
			return;
		}

#if DEBUG
		(this as IPluginLogger)?.Logger.Debug($"Disabling {Hooks?.Where(hook => hook.IsEnabled).Count() ?? 0}/{Hooks?.Count ?? 0} hook(s)");
#endif

		Hooks?.ForEach(hook => hook.Disable());
	}

	void DisposeHooks()
	{
		if (!Hooks?.Any() ?? true)
		{
			return;
		}

#if DEBUG
		(this as IPluginLogger)?.Logger.Debug($"Disposing {Hooks?.Where(hook => !hook.IsDisposed).Count() ?? 0}/{Hooks?.Count ?? 0} hook(s)");
#endif

		Hooks?.ForEach(hook => hook.Dispose());
	}

	HookWrapper<T>? Hook<T>(string signature, T detour, int addressOffset = 0, bool failable = false) where T : Delegate
	{
		if (!Services.SigScanner.TryScanText(signature, out IntPtr address))
		{
			(this as IPluginLogger)?.Logger.Error($"Failed to find {detour.GetType()} hook target address with signature {signature}");

			if (failable || this is not IPluginComponent)
			{
				return null;
			}

			(this as IPluginLogger)?.Logger.Error($"Cannot load {GetType()}");
			((IPluginComponent) this).CanLoad = false;
			return null;
		}

#if DEBUG
		(this as IPluginLogger)?.Logger.Debug($"Found signature {signature} at 0x{address.ToInt64():X} for target {detour.GetType()}");
#endif

		return Hook(address + addressOffset, detour);
	}

	unsafe HookWrapper<T> Hook<T>(void* address, T detour) where T : Delegate => Hook(new IntPtr(address), detour);

	HookWrapper<T> Hook<T>(IntPtr address, T detour) where T : Delegate
	{
#if DEBUG
		(this as IPluginLogger)?.Logger.Debug($"Hooking 0x{address.ToInt64():X} with target {detour.GetType()}");
#endif

		Hooks ??= new();
		Hook<T> hook = Services.HookProvider.HookFromAddress(address, detour);

		HookWrapper<T> wrappedHook = new(hook);
		Hooks.Add(wrappedHook);
		return wrappedHook;
	}
}