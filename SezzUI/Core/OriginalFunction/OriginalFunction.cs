using System;
using Dalamud.Logging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Reloaded.Memory.Sources;
using static Reloaded.Memory.Sources.Memory;
using Iced.Intel;
using SezzUI.Helpers;

namespace SezzUI.Hooking
{
	/// <summary>
	/// While this may look generic at first it is exclusively for GetAdjustedActionId.
	/// DO NOT USE THIS FOR ANYTHING ELSE without implementing unsupported opcodes. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OriginalFunction<T> : IDisposable where T : Delegate
	{
		private readonly IntPtr _originalPointer;
		public IntPtr OriginalAddress => _originalPointer;
		private byte[] _originalBytes;

		private IntPtr _newPointer = IntPtr.Zero;
		public IntPtr Address => _newPointer;

		private static readonly int minHookLength = 8; // Usually 8
		private static readonly int maxHookLength = 16; // Usually 8
		private static readonly int defaultHookLength = 8; // If the original function isn't hooked we assume that the hook will be of that size and only copy those instructions.
		private static readonly int maxHookInstructions = 4; // Usually 1
		private static readonly int maxFunctionSize = 16 * 2;

		public T? Invoke { get; private set; }

		#region Constructor
		public OriginalFunction(string signature, string originalBytesString = "")
		{
			_originalPointer = Plugin.SigScanner.ScanText(signature);
			_originalBytes = Convert.FromHexString(AsmHelper.CleanHexString(originalBytesString));
			Initialize();
		}

		public OriginalFunction(string signature, byte[] originalBytes)
		{
			_originalPointer = Plugin.SigScanner.ScanText(signature);
			_originalBytes = originalBytes;
			Initialize();
		}

		public OriginalFunction(IntPtr originalPointer, string originalBytesString = "")
		{
			_originalPointer = originalPointer;
			_originalBytes = Convert.FromHexString(AsmHelper.CleanHexString(originalBytesString));
			Initialize();
		}

		public OriginalFunction(IntPtr originalPointer, byte[] originalBytes)
		{
			_originalPointer = originalPointer;
			_originalBytes = originalBytes;
			Initialize();
		}
		#endregion

		private void Initialize()
		{
			PluginLog.Debug($"Original address: {_originalPointer.ToInt64():X}");

			// Read current memory
			CurrentProcess.SafeReadRaw(_originalPointer, out byte[] currentBytes, maxHookLength);
			PluginLog.Debug("Byte code: " + Convert.ToHexString(currentBytes));

			// Decode current memory
			List<Instruction> currentInstructions = AsmHelper.DecodeInstructions(currentBytes, _originalPointer);
			if (currentInstructions.Count == 0)
			{
				throw new Exception("Failed to disassemble current instructions!");
			}

			bool isHooked = currentInstructions.Any() && currentInstructions[0].Mnemonic == Mnemonic.Jmp; // TODO: This only works if the original function doesn't start with a JMP!
			int hookLength = 0;
			if (!isHooked)
			{
				currentInstructions.ForEach(instr => hookLength += instr.Length);

				if (_originalBytes.Length == 0)
				{
					// Use current byte code
					_originalBytes = currentBytes[0..hookLength];
				}
				else
				{
					// Compare if supplied original code is different, just to be safe.
					var length = Math.Min(_originalBytes.Length, currentBytes.Length);
					if (!_originalBytes[0..length].SequenceEqual(currentBytes[0..length]))
					{
						throw new Exception("Supplied original byte code doesn't match byte code in memory!");
					}

					hookLength = defaultHookLength;
				}

				// TODO: Just hook it?
			}

			if (_originalBytes.Length < minHookLength)
			{
				throw new Exception("Original byte code size is smaller than minimum hook length!");
			}

			// Find end of hook instructions
			bool foundHookEnd = false;
			if (isHooked)
			{
				PluginLog.Debug($"Original function is already hooked.");

				for (int i = 0; i < currentInstructions.Count; i++)
				{
					if (!foundHookEnd && i > 0 && currentInstructions[i].Mnemonic != Mnemonic.Nop)
					{
						foundHookEnd = true;
						break;
					}
					hookLength += currentInstructions[i].Length;

					if (i > maxHookInstructions)
					{
						throw new Exception("Failed to lookup end of current hook!");
					}
				}
			}

			if (isHooked && !foundHookEnd)
			{
				throw new Exception("Failed to lookup end of current hook!");
			}

			if (!isHooked && !foundHookEnd && hookLength == 0)
			{
				hookLength = defaultHookLength;
			}

			PluginLog.Debug($">>> Length of hook instructions: 0x{hookLength:X}");

			// Dump
			PluginLog.Debug(">>> Current instructions:");
			AsmHelper.DumpInstructions(currentBytes[0..hookLength], _originalPointer);

			PluginLog.Debug(">>> Original instructions:");
			AsmHelper.DumpInstructions(_originalBytes[0..hookLength], _originalPointer);

			// Create new instructions
			var minMax = Reloaded.Hooks.Tools.Utilities.GetRelativeJumpMinMax((long)_originalPointer, Int32.MaxValue - maxFunctionSize);
			var buffer = Reloaded.Hooks.Tools.Utilities.FindOrCreateBufferInRange(maxFunctionSize, minMax.min, minMax.max, 1);
			List<byte> opCodes = new();

			_newPointer = buffer.ExecuteWithLock(() =>
			{
				buffer.SetAlignment(16);
				IntPtr currAddress = buffer.Properties.WritePointer;

				List<string> assemblyCode = new()
				{
					"use64",
					$"org {currAddress}"
				};

				var originalInstructions = AsmHelper.DecodeInstructions(_originalBytes[0..hookLength], _originalPointer);
				foreach (var instruction in originalInstructions)
				{
					// TODO: I'm too lazy right now to check those I don't need.
					assemblyCode.Add(instruction.OpCode.Mnemonic == Mnemonic.Jg ? $"jg qword {(IntPtr) instruction.NearBranchTarget}" : instruction.ToString());
				}

				assemblyCode.Add($"jmp qword {_originalPointer + hookLength}");
				opCodes.AddRange(Reloaded.Hooks.Tools.Utilities.Assembler.Assemble(assemblyCode.ToArray()));
				Reloaded.Hooks.Tools.Utilities.FillArrayUntilSize<byte>(opCodes, 0x90, maxFunctionSize);

				return buffer.Add(opCodes.ToArray(), 1);
			});

			// Dump
			PluginLog.Debug(">>> New instructions:");
			AsmHelper.DumpInstructions(opCodes.ToArray(), _newPointer);
			PluginLog.Debug("New byte code: " + Convert.ToHexString(opCodes.ToArray()));
			PluginLog.Debug($"New address: {_newPointer.ToInt64():X}");

			// Done
			Invoke = Marshal.GetDelegateForFunctionPointer<T>(_newPointer);
		}

		/// <summary>
		/// Gets a value indicating whether or not the hook has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		public void Dispose()
		{
			if (IsDisposed) { return; }

			if (_newPointer != IntPtr.Zero)
			{
				//MemoryHelper.BufferHelper.Free(_newPointer);
			}

			IsDisposed = true;
		}
	}
}
