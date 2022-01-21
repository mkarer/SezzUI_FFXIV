using System;
using Dalamud.Logging;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Iced.Intel;

namespace SezzUI.Helpers
{
	public static class AsmHelper
	{
		public static string CleanHexString(string s) => Regex.Replace(s, "[^A-Fa-f0-9]*", "");

		public static void DumpInstructions(byte[] bytes, IntPtr ip)
		{
			var instructions = DecodeInstructions(bytes, ip);
			string pad = "D" + instructions.Count.ToString("D").Length;
			for (int i = 0; i < instructions.Count; i++)
			{
				PluginLog.Debug($"Instruction {i.ToString(pad)}: {instructions[i]}");
			}
		}

		public static List<Instruction> DecodeInstructions(byte[] bytes, IntPtr ip)
		{
			var decoder = Decoder.Create(64, bytes);
			decoder.IP = (ulong)ip;
			ulong endRip = decoder.IP + (uint)bytes.Length;

			var instructions = new List<Instruction>();
			while (decoder.IP < endRip)
			{
				var instr = decoder.Decode();
				if (instr.IsInvalid) { break; }
				instructions.Add(instr);
			}

			return instructions;
		}
	}
}
