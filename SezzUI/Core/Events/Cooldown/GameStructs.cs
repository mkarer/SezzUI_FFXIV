using System.Runtime.InteropServices;

namespace SezzUI.GameStructs
{
	// https://github.com/yomishino/FFXIVActionEffectRange/blob/master/src/Actions/ActionWatcher.cs
	[StructLayout(LayoutKind.Explicit)]
	public struct ActionEffectHeader
	{
		[FieldOffset(0x0)]
		public long TargetObjectId;

		[FieldOffset(0x8)]
		public uint ActionId;

		// Unk; but have some value keep accumulating here
		[FieldOffset(0x14)]
		public uint UnkObjectId;

		[FieldOffset(0x18)]
		public ushort Sequence; // Corresponds exactly to the sequence of the action used; AA, pet's action effect etc. will be 0 here

		[FieldOffset(0x1A)]
		public ushort Unk_1A; // Seems related to SendAction's arg a5, but not always the same value
		// rest??
	}
}