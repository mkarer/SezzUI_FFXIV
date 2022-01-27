using System.Runtime.InteropServices;

namespace SezzUI.GameStructs
{
	// https://github.com/yomishino/FFXIVActionEffectRange/blob/master/src/Actions/ActionWatcher.cs
	// https://github.com/0ceal0t/JobBars/blob/main/JobBars/JobBars.Hooks.cs
	// https://github.com/lmcintyre/DamageInfoPlugin/blob/main/DamageInfoPlugin/DamageInfoPlugin.cs
	[StructLayout(LayoutKind.Explicit)]
	public struct ActionEffectHeader
	{
		[FieldOffset(0x0)]
		public long TargetObjectId;

		[FieldOffset(0x2)]
		public uint EffectId;

		[FieldOffset(0x8)]
		public uint ActionId;

		[FieldOffset(0x10)]
		public float AnimationLock;

		// Unk; but have some value keep accumulating here
		[FieldOffset(0x14)]
		public uint UnkObjectId;

		[FieldOffset(0x18)]
		public ushort Sequence; // Corresponds exactly to the sequence of the action used; AA, pet's action effect etc. will be 0 here

		[FieldOffset(0x1a)]
		public ushort Unk_1a; // Seems related to SendAction's arg a5, but not always the same value

		[FieldOffset(0x1c)]
		public ushort AnimationId;

		[FieldOffset(0x1f)]
		public byte Type;

		[FieldOffset(0x21)]
		public byte TargetCount;
	}
}