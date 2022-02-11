using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using FFXIVClientStructs.Attributes;
using Iced.Intel;
using SezzUI.Logging;

namespace SezzUI.Helper
{
	public static class AsmHelper
	{
		internal static PluginLogger Logger;

		static AsmHelper()
		{
			Logger = new("AsmHelper");
		}

		public static string CleanHexString(string s) => Regex.Replace(s, "[^A-Fa-f0-9]*", "");

		public static void DumpInstructions(byte[] bytes, IntPtr ip)
		{
			List<Instruction> instructions = DecodeInstructions(bytes, ip);
			string pad = "D" + instructions.Count.ToString("D").Length;
			for (int i = 0; i < instructions.Count; i++)
			{
				Logger.Debug($"Instruction {i.ToString(pad)}: {instructions[i]}");
			}
		}

		public static List<Instruction> DecodeInstructions(byte[] bytes, IntPtr ip)
		{
			Decoder decoder = Decoder.Create(64, bytes);
			decoder.IP = (ulong) ip;
			ulong endRip = decoder.IP + (uint) bytes.Length;

			List<Instruction> instructions = new();
			while (decoder.IP < endRip)
			{
				Instruction instr = decoder.Decode();
				if (instr.IsInvalid)
				{
					break;
				}

				instructions.Add(instr);
			}

			return instructions;
		}

		public static string? GetSignature<T>(string methodName)
		{
			// https://github.com/CaiClone/GCDTracker/blob/main/src/Data/HelperMethods.cs
			MethodBase? method = typeof(T).GetMethod(methodName);
			if (method == null)
			{
				return null;
			}

			MemberFunctionAttribute attribute = (MemberFunctionAttribute) method.GetCustomAttributes(typeof(MemberFunctionAttribute), true)[0];
			return attribute?.Signature ?? null;
		}
	}
}