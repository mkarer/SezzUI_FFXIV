/*
Copyright(c) 2021 jkcclemens HUD Manager
Modifications Copyright(c) 2021 DelvUI
09/27/2021 - Extracted code to move in-game UI elements.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using SezzUI.Config;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Interface
{
	public class HudHelper : IDisposable
	{
		private delegate void SetPositionDelegate(IntPtr addon, short x, short y);

		private delegate IntPtr GetBaseUIObjectDelegate();

		private delegate byte UpdateAddonPositionDelegate(IntPtr manager, IntPtr addon, byte clicked);

		private delegate IntPtr GetFilePointerDelegate(byte index);

		private HUDOptionsConfig Config => ConfigurationManager.Instance.GetConfigObject<HUDOptionsConfig>();

		private readonly GetBaseUIObjectDelegate? _getBaseUIObject;
		private readonly SetPositionDelegate? _setPosition;
		private readonly UpdateAddonPositionDelegate? _updateAddonPosition;
		private readonly GetFilePointerDelegate? _getFilePointer;

		public HudHelper()
		{
			#region Signatures

			Config.ValueChangeEvent += ConfigValueChanged;

			/*
			Part of getBaseUiObject disassembly signature
			.text:00007FF6481C2F60                   Component__GUI__AtkStage_GetSingleton1 proc near
			.text:00007FF6481C2F60 48 8B 05 99 04 8D+mov     rax, cs:g_AtkStage
			.text:00007FF6481C2F60 01
			.text:00007FF6481C2F67 C3                retn
			.text:00007FF6481C2F67                   Component__GUI__AtkStage_GetSingleton1 endp
			.text:00007FF6481C2F67
			*/
			IntPtr getBaseUiObjectPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F BF D5");
			_getBaseUIObject = Marshal.GetDelegateForFunctionPointer<GetBaseUIObjectDelegate>(getBaseUiObjectPtr);

			/*
			Part of setPosition disassembly signature
			.text:00007FF6481BFF20                   Component__GUI__AtkUnitBase_SetPosition proc near
			.text:00007FF6481BFF20 4C 8B 89 C8 00 00+mov     r9, [rcx+0C8h]
			.text:00007FF6481BFF20 00
			.text:00007FF6481BFF27 41 0F BF C0       movsx   eax, r8w
			.text:00007FF6481BFF2B 66 89 91 BC 01 00+mov     [rcx+1BCh], dx
			.text:00007FF6481BFF2B 00
			.text:00007FF6481BFF32 66 44 89 81 BE 01+mov     [rcx+1BEh], r8w
			.text:00007FF6481BFF32 00 00
			.text:00007FF6481BFF3A 66 0F 6E C8       movd    xmm1, eax
			.text:00007FF6481BFF3E 0F BF C2          movsx   eax, dx
			.text:00007FF6481BFF41 0F 5B C9          cvtdq2ps xmm1, xmm1
			.text:00007FF6481BFF44 66 0F 6E D0       movd    xmm2, eax
			.text:00007FF6481BFF48 0F 5B D2          cvtdq2ps xmm2, xmm2
			.text:00007FF6481BFF4B 4D 85 C9          test    r9, r9
			.text:00007FF6481BFF4E 74 3B             jz      short locret_7FF6481BFF8B
			*/
			IntPtr setPositionPtr = Plugin.SigScanner.ScanText("4C 8B 89 ?? ?? ?? ?? 41 0F BF C0");
			_setPosition = Marshal.GetDelegateForFunctionPointer<SetPositionDelegate>(setPositionPtr);

			/*
			Part of updateAddonPosition disassembly signature
			.text:00007FF6481CF020                   sub_7FF6481CF020 proc near
			.text:00007FF6481CF020
			.text:00007FF6481CF020                   arg_0= qword ptr  8
			.text:00007FF6481CF020
			.text:00007FF6481CF020 48 89 5C 24 08    mov     [rsp+arg_0], rbx
			.text:00007FF6481CF025 57                push    rdi
			.text:00007FF6481CF026 48 83 EC 20       sub     rsp, 20h
			.text:00007FF6481CF02A 48 8B DA          mov     rbx, rdx
			.text:00007FF6481CF02D 48 8B F9          mov     rdi, rcx
			.text:00007FF6481CF030 48 85 D2          test    rdx, rdx
			.text:00007FF6481CF033 0F 84 CA 00 00 00 jz      loc_7FF6481CF103
			*/
			IntPtr updateAddonPositionPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 33 D2 48 8B 01 FF 90 ?? ?? ?? ??");
			_updateAddonPosition = Marshal.GetDelegateForFunctionPointer<UpdateAddonPositionDelegate>(updateAddonPositionPtr);

			IntPtr getFilePointerPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00");
			if (getFilePointerPtr != IntPtr.Zero)
			{
				_getFilePointer = Marshal.GetDelegateForFunctionPointer<GetFilePointerDelegate>(getFilePointerPtr);
			}

			#endregion
		}

		internal static byte GetStatus(GameObject actor)
		{
			// 40 57 48 83 EC 70 48 8B F9 E8 ?? ?? ?? ?? 81 BF ?? ?? ?? ?? ?? ?? ?? ??
			const int offset = 0x19A0;
			return Marshal.ReadByte(actor.Address + offset);
		}

		internal int GetActiveHUDLayoutIndex()
		{
			if (_getFilePointer == null)
			{
				return 0;
			}

			IntPtr dataPtr = _getFilePointer.Invoke(0) + 0x50;
			IntPtr slotPtr = Marshal.ReadIntPtr(dataPtr) + 0x59e8;
			int index = Marshal.ReadInt32(slotPtr);

			return Math.Clamp(index, 0, 3);
		}

		~HudHelper()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			Config.ValueChangeEvent -= ConfigValueChanged;
		}

		public void Update()
		{
		}

		public bool IsElementHidden(HudElement element)
		{
			if (!ConfigurationManager.Instance.LockHUD)
			{
				return false;
			}

			if (!element.GetConfig().Enabled)
			{
				return true;
			}

			return false;
		}

		private void ConfigValueChanged(object sender, OnChangeBaseArgs e)
		{
		}
	}
}