using System;
using System.Runtime.InteropServices;

namespace Monitorian.Helper
{
	internal static class HotKeyHelper
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		internal static extern bool RegisterHotKey(
			IntPtr hWnd,
			int id,
			KeyModifiers fsModifiers,
			System.Windows.Forms.Keys vk);

		[Flags]
		internal enum KeyModifiers : uint
		{
			MOD_ALT = 0x0001,
			MOD_CONTROL = 0x0002,
			MOD_SHIFT = 0x0004,
			MOD_WIN = 0x0008,
			MOD_NOREPEAT = 0x4000,
		}

		[DllImport("User32.dll", SetLastError = true)]
		internal static extern bool UnregisterHotKey(
			IntPtr hWnd,
			int id);

		internal const int WM_HOTKEY = 0x0312;
		internal const int IDHOT_SNAPDESKTOP = -2;
		internal const int IDHOT_SNAPWINDOW = -1;

		#endregion
	}

	internal class HotKeyTuple
	{
		public int Id { get; set; }
		public System.Windows.Forms.Keys VirtualKey { get; set; }
		public HotKeyHelper.KeyModifiers KeyModifiers { get; set; }
	}

	internal class HotKeyEventArgs : EventArgs
	{
		public HotKeyEventArgs(HotKeyTuple args)
		{
			_args = args;
		}

		private readonly HotKeyTuple _args;

		public int Id { get => _args.Id; }
		public System.Windows.Forms.Keys VirtualKey { get => _args.VirtualKey; }
		public HotKeyHelper.KeyModifiers KeyModifiers { get => _args.KeyModifiers; }
	}

	internal delegate void HotKeyEventHandler(object sender, HotKeyEventArgs e);
}
