using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Watcher
{
	/// <summary>
	/// Complement class for <see cref="Microsoft.Win32.SystemEvents"/>
	/// </summary>
	internal class SystemEventsComplement
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr RegisterPowerSettingNotification(
			IntPtr hRecipient,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid PowerSettingGuid,
			uint Flags);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnregisterPowerSettingNotification(
			IntPtr Handle);

		[StructLayout(LayoutKind.Sequential)]
		private struct POWERBROADCAST_SETTING
		{
			public Guid PowerSetting;
			public uint DataLength;
			public IntPtr Data;
		}

		private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
		private const int WM_POWERBROADCAST = 0x0218;
		private const int PBT_POWERSETTINGCHANGE = 0x8013;

		#endregion

		private class EventWindow : NativeWindow
		{
			private readonly Action<PowerSettingChangedEventArgs> _action;

			public EventWindow(IntPtr handle, Action<PowerSettingChangedEventArgs> action)
			{
				this.AssignHandle(handle);
				this._action = action ?? throw new ArgumentNullException(nameof(action));
			}

			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
					case WM_POWERBROADCAST:
						if (m.WParam.ToInt32() == PBT_POWERSETTINGCHANGE)
						{
							var data = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(m.LParam);
							var buffer = (data.DataLength == 4 /* DWORD */) ? data.Data.ToInt32() : 0;
							_action.Invoke(new PowerSettingChangedEventArgs(data.PowerSetting, buffer));
						}
						break;
				}
				base.WndProc(ref m);
			}
		}

		public event EventHandler<PowerSettingChangedEventArgs> PowerSettingChanged;

		private List<IntPtr> _registrationHandles;
		private EventWindow _eventWindow;

		public void RegisterPowerSettingEvent(IReadOnlyCollection<Guid> settingGuids)
		{
			if (settingGuids?.Any() is not true)
				return;

			if (!TryGetSystemEventsWindowHandle(out IntPtr windowHandle))
				return;

			_registrationHandles ??= new List<IntPtr>();

			foreach (var guid in settingGuids)
			{
				var handle = RegisterPowerSettingNotification(
					windowHandle,
					guid,
					DEVICE_NOTIFY_WINDOW_HANDLE);
				if (handle != IntPtr.Zero)
					_registrationHandles.Add(handle);
			}

			TryEnsureSystemEvents();

			_eventWindow ??= new EventWindow(windowHandle, (e) => PowerSettingChanged?.Invoke(this, e));
		}

		private static bool TryGetSystemEventsWindowHandle(out IntPtr windowHandle)
		{
			var systemEventsField = typeof(SystemEvents).GetField("systemEvents", BindingFlags.Static | BindingFlags.NonPublic);
			var instance = systemEventsField?.GetValue(null); // Static field
			if (instance is null)
			{
				windowHandle = default;
				return false;
			}

			var windowHandleField = typeof(SystemEvents).GetField("windowHandle", BindingFlags.Instance | BindingFlags.NonPublic);
			var handle = windowHandleField?.GetValue(instance);
			if (handle is null)
			{
				windowHandle = default;
				return false;
			}

			windowHandle = (IntPtr)handle;
			return true;
		}

		private static bool TryEnsureSystemEvents()
		{
			var ensureSystemEventsMethod = typeof(SystemEvents).GetMethod("EnsureSystemEvents", BindingFlags.Static | BindingFlags.NonPublic);
			if (ensureSystemEventsMethod is null)
				return false;

			ensureSystemEventsMethod.Invoke(null, new object[] { true, true });
			return true;
		}

		public void UnregisterPowerSettingEvent()
		{
			PowerSettingChanged = null;

			if (_registrationHandles?.Any() is true)
			{
				foreach (var handle in _registrationHandles)
					UnregisterPowerSettingNotification(handle);

				_registrationHandles.Clear();
			}

			_eventWindow?.ReleaseHandle();
		}
	}

	public class PowerSettingChangedEventArgs : EventArgs
	{
		public Guid Guid { get; }
		public int Data { get; }

		public PowerSettingChangedEventArgs(Guid guid, int data) => (this.Guid, this.Data) = (guid, data);
	}
}