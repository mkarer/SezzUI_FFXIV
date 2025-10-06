using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Iced.Intel;
using Reloaded.Hooks.Tools;
using Reloaded.Memory.Buffers;
using Reloaded.Memory.Sources;
using SezzUI.Helper;
using SezzUI.Logging;
using static Reloaded.Memory.Sources.Memory;

namespace SezzUI.Hooking;

/// <summary>
///     While this may look generic at first it is exclusively for GetAdjustedActionId.
///     DO NOT USE THIS FOR ANYTHING ELSE without implementing unsupported opcodes.
/// </summary>
/// <typeparam name="T"></typeparam>
public class OriginalFunction<T> : IDisposable where T : Delegate
{
	internal PluginLogger Logger = null!;

	public IntPtr OriginalAddress { get; }

	private byte[] _originalBytes;

	public IntPtr Address { get; private set; } = IntPtr.Zero;

	private const int MIN_HOOK_LENGTH = 8; // Usually 8
	private const int MAX_HOOK_LENGTH = 16; // Usually 8
	private const int DEFAULT_HOOK_LENGTH = 8; // If the original function isn't hooked we assume that the hook will be of that size and only copy those instructions.
	private const int MAX_HOOK_INSTRUCTIONS = 4; // Usually 1
	private const int MAX_FUNCTION_SIZE = 16 * 2;

	public T? Invoke { get; private set; }

	#region Constructor

	private void InitializeLogger()
	{
		Logger = new(GetType().Name + ":" + typeof(T).Name);
	}

	public OriginalFunction(string signature, string originalBytesString = "")
	{
		InitializeLogger();
		OriginalAddress = Services.SigScanner.ScanText(signature);
		_originalBytes = Convert.FromHexString(AsmHelper.CleanHexString(originalBytesString));
		Initialize();
	}

	public OriginalFunction(string signature, byte[] originalBytes)
	{
		InitializeLogger();
		OriginalAddress = Services.SigScanner.ScanText(signature);
		_originalBytes = originalBytes;
		Initialize();
	}

	public OriginalFunction(IntPtr originalPointer, string originalBytesString = "")
	{
		InitializeLogger();
		OriginalAddress = originalPointer;
		_originalBytes = Convert.FromHexString(AsmHelper.CleanHexString(originalBytesString));
		Initialize();
	}

	public OriginalFunction(IntPtr originalPointer, byte[] originalBytes)
	{
		InitializeLogger();
		OriginalAddress = originalPointer;
		_originalBytes = originalBytes;
		Initialize();
	}

	#endregion

	private void Initialize()
	{
#if DEBUG
		if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsOriginalFunctionManager)
		{
			Logger.Debug($"Original address: {OriginalAddress.ToInt64():X}");
		}
#endif
		// Read current memory
		CurrentProcess.SafeReadRaw((nuint) OriginalAddress, out byte[] currentBytes, MAX_HOOK_LENGTH);
#if DEBUG
		if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsOriginalFunctionManager)
		{
			Logger.Debug("Byte code: " + Convert.ToHexString(currentBytes));
		}
#endif

		// Decode current memory
		List<Instruction> currentInstructions = AsmHelper.DecodeInstructions(currentBytes, OriginalAddress);
		if (currentInstructions.Count == 0)
		{
			throw new("Failed to disassemble current instructions!");
		}

		bool isHooked = currentInstructions.Any() && currentInstructions[0].Mnemonic == Mnemonic.Jmp; // TODO: This only works if the original function doesn't start with a JMP!
		int hookLength = 0;
		if (!isHooked)
		{
			currentInstructions.ForEach(instr => hookLength += instr.Length);

			if (_originalBytes.Length == 0)
			{
				// Use current byte code
				_originalBytes = currentBytes[..hookLength];
			}
			else
			{
				// Compare if supplied original code is different, just to be safe.
				int length = Math.Min(_originalBytes.Length, currentBytes.Length);
				if (!_originalBytes[..length].SequenceEqual(currentBytes[..length]))
				{
					throw new("Supplied original byte code doesn't match byte code in memory!");
				}

				hookLength = DEFAULT_HOOK_LENGTH;
			}

			// TODO: Just hook it?
		}

		if (_originalBytes.Length < MIN_HOOK_LENGTH)
		{
			throw new("Original byte code size is smaller than minimum hook length!");
		}

		// Find end of hook instructions
		bool foundHookEnd = false;
		if (isHooked)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsOriginalFunctionManager)
			{
				Logger.Debug("Original function is already hooked.");
			}
#endif
			for (int i = 0; i < currentInstructions.Count; i++)
			{
				if (!foundHookEnd && i > 0 && currentInstructions[i].Mnemonic != Mnemonic.Nop)
				{
					foundHookEnd = true;
					break;
				}

				hookLength += currentInstructions[i].Length;

				if (i > MAX_HOOK_INSTRUCTIONS)
				{
					throw new("Failed to lookup end of current hook!");
				}
			}
		}

		hookLength = isHooked switch
		{
			true when !foundHookEnd => throw new("Failed to lookup end of current hook!"),
			false when !foundHookEnd && hookLength == 0 => DEFAULT_HOOK_LENGTH,
			_ => hookLength
		};

#if DEBUG
		if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsOriginalFunctionManager)
		{
			Logger.Debug($"Length of instructions we're going to skip: 0x{hookLength:X}");

			// Dump
			Logger.Debug("Current instructions:");
			AsmHelper.DumpInstructions(currentBytes[..hookLength], OriginalAddress);

			Logger.Debug("Original instructions:");
			AsmHelper.DumpInstructions(_originalBytes[..hookLength], OriginalAddress);
		}
#endif

		// Create new instructions
		(nuint min, nuint max) = Utilities.GetRelativeJumpMinMax((nuint) OriginalAddress, int.MaxValue - MAX_FUNCTION_SIZE);
		MemoryBuffer buffer = Utilities.FindOrCreateBufferInRange(MAX_FUNCTION_SIZE, min, max, 1);
		List<byte> opCodes = new();

		Address = new((nint) buffer.ExecuteWithLock(() =>
		{
			buffer.SetAlignment(16);

			List<string> assemblyCode = new()
			{
				"use64",
				$"org {buffer.Properties.WritePointer}"
			};

			List<Instruction> originalInstructions = AsmHelper.DecodeInstructions(_originalBytes[..hookLength], OriginalAddress);
			// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
			foreach (Instruction instruction in originalInstructions)
			{
				// TODO: I'm too lazy right now to check those I don't need.
				assemblyCode.Add(instruction.OpCode.Mnemonic == Mnemonic.Jg ? $"jg qword {(IntPtr) instruction.NearBranchTarget}" : instruction.ToString());
			}

			assemblyCode.Add($"jmp qword {OriginalAddress + hookLength}");
			opCodes.AddRange(Utilities.Assemble(assemblyCode.ToArray()));
			Utilities.FillArrayUntilSize<byte>(opCodes, 0x90, MAX_FUNCTION_SIZE);

			return buffer.Add(opCodes.ToArray(), 1);
		}));

		// Dump
#if DEBUG
		if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsOriginalFunctionManager)
		{
			Logger.Debug("New instructions:");
			AsmHelper.DumpInstructions(opCodes.ToArray(), Address);
			Logger.Debug("New byte code: " + Convert.ToHexString(opCodes.ToArray()));
			Logger.Debug($"New address: {Address.ToInt64():X}");
		}
#endif

		// Done
		Invoke = Marshal.GetDelegateForFunctionPointer<T>(Address);
	}

	/// <summary>
	///     Gets a value indicating whether or not the hook has been disposed.
	/// </summary>
	private bool IsDisposed { get; set; }

	public void Dispose()
	{
		if (IsDisposed)
		{
			return;
		}

		if (Address != IntPtr.Zero)
		{
			//MemoryHelper.BufferHelper.Free(_newPointer);
		}

		IsDisposed = true;
	}
}