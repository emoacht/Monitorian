using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor
{
	internal class DeviceContext
	{
		#region Win32

		[DllImport("User32.dll")]
		private static extern bool EnumDisplayMonitors(
			IntPtr hdc,
			IntPtr lprcClip,
			MonitorEnumProc lpfnEnum,
			IntPtr dwData);

		[return: MarshalAs(UnmanagedType.Bool)]
		private delegate bool MonitorEnumProc(
			IntPtr hMonitor,
			IntPtr hdcMonitor,
			IntPtr lprcMonitor,
			IntPtr dwData);

		[DllImport("User32.dll", EntryPoint = "GetMonitorInfoW")]
		private static extern bool GetMonitorInfo(
			IntPtr hMonitor,
			ref MONITORINFOEX lpmi);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct MONITORINFOEX
		{
			public uint cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szDevice;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[DllImport("User32.dll", EntryPoint = "EnumDisplayDevicesA")]
		private static extern bool EnumDisplayDevices(
			string lpDevice,
			uint iDevNum,
			ref DISPLAY_DEVICE lpDisplayDevice,
			uint dwFlags);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private struct DISPLAY_DEVICE
		{
			public uint cb;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string DeviceName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceString;

			public DISPLAY_DEVICE_FLAG StateFlags;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceID;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceKey;
		}

		[Flags]
		private enum DISPLAY_DEVICE_FLAG : uint
		{
			DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001,
			DISPLAY_DEVICE_MULTI_DRIVER = 0x00000002,
			DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004,
			DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008,
			DISPLAY_DEVICE_VGA_COMPATIBLE = 0x00000010,
			DISPLAY_DEVICE_REMOVABLE = 0x00000020,
			DISPLAY_DEVICE_ACC_DRIVER = 0x00000040,
			DISPLAY_DEVICE_RDPUDD = 0x01000000,
			DISPLAY_DEVICE_DISCONNECT = 0x02000000,
			DISPLAY_DEVICE_REMOTE = 0x04000000,
			DISPLAY_DEVICE_MODESPRUNED = 0x08000000,

			DISPLAY_DEVICE_ACTIVE = 0x00000001,
			DISPLAY_DEVICE_ATTACHED = 0x00000002,
		}

		[DllImport("User32.dll", EntryPoint = "EnumDisplaySettingsA")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool EnumDisplaySettings(
			string lpszDeviceName,
			int iModeNum,
			ref DEVMODE lpDevMode);

		[DllImport("User32.dll", EntryPoint = "ChangeDisplaySettingsExA")]
		private static extern int ChangeDisplaySettingsEx(
			string lpszDeviceName,
			[In] ref DEVMODE lpDevMode,
			IntPtr hwnd, // Always null
			uint dwflags,
			IntPtr lParam);

		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
		private struct DEVMODE
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			[FieldOffset(0)] public string dmDeviceName;

			[FieldOffset(36)] public ushort dmSize;

			[FieldOffset(40)] public DM dmFields; // uint

			[FieldOffset(52)] public DMDO dmDisplayOrientation; // uint

			[FieldOffset(102)] public ushort dmLogPixels;
			[FieldOffset(104)] public uint dmBitsPerPel;
			[FieldOffset(108)] public uint dmPelsWidth;
			[FieldOffset(112)] public uint dmPelsHeight;
		}

		[Flags]
		private enum DM : uint
		{
			DM_DISPLAYORIENTATION = 0x00000080,
			DM_LOGPIXELS = 0x00020000,
			DM_BITSPERPEL = 0x00040000,
			DM_PELSWIDTH = 0x00080000,
			DM_PELSHEIGHT = 0x00100000
		}

		/// <summary>
		/// DEVMODE Display Orientation (anti-clockwise from user's viewpoint) 
		/// </summary>
		private enum DMDO : uint
		{
			DMDO_DEFAULT = 0,
			DMDO_90 = 1,
			DMDO_180 = 2,
			DMDO_270 = 3
		}

		private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;
		private const int ENUM_CURRENT_SETTINGS = -1;
		private const int DISP_CHANGE_SUCCESSFUL = 0;

		#endregion

		#region Type

		[DataContract]
		public class DeviceItem
		{
			[DataMember(Order = 0)]
			public string DeviceInstanceId { get; private set; }

			[DataMember(Order = 1)]
			public string Description { get; private set; }

			[DataMember(Order = 2)]
			public byte DisplayIndex { get; private set; }

			[DataMember(Order = 3)]
			public byte MonitorIndex { get; private set; }

			public DeviceItem(
				string deviceInstanceId,
				string description,
				byte displayIndex,
				byte monitorIndex)
			{
				this.DeviceInstanceId = deviceInstanceId;
				this.Description = description;
				this.DisplayIndex = displayIndex;
				this.MonitorIndex = monitorIndex;
			}
		}

		[DataContract]
		public class HandleItem
		{
			[DataMember]
			public int DisplayIndex { get; private set; }

			public IntPtr MonitorHandle { get; }

			public HandleItem(
				int displayIndex,
				IntPtr monitorHandle)
			{
				this.DisplayIndex = displayIndex;
				this.MonitorHandle = monitorHandle;
			}
		}

		#endregion

		public static IEnumerable<DeviceItem> EnumerateMonitorDevices()
		{
			foreach (var (_, displayIndex, monitor, monitorIndex) in EnumerateDevices())
			{
				var deviceInstanceId = GetDeviceInstanceId(monitor.DeviceID);

				//Debug.WriteLine($"DeviceId: {monitor.DeviceID}");
				//Debug.WriteLine($"DeviceInstanceId: {deviceInstanceId}");
				//Debug.WriteLine($"DeviceString: {monitor.DeviceString}");

				yield return new DeviceItem(
					deviceInstanceId: deviceInstanceId,
					description: monitor.DeviceString,
					displayIndex: displayIndex,
					monitorIndex: monitorIndex);
			}
		}

		private static IEnumerable<(DISPLAY_DEVICE display, byte displayIndex, DISPLAY_DEVICE monitor, byte monitorIndex)> EnumerateDevices()
		{
			var size = (uint)Marshal.SizeOf<DISPLAY_DEVICE>();
			var display = new DISPLAY_DEVICE { cb = size };
			var monitor = new DISPLAY_DEVICE { cb = size };

			for (uint i = 0; EnumDisplayDevices(null, i, ref display, EDD_GET_DEVICE_INTERFACE_NAME); i++)
			{
				if (display.StateFlags.HasFlag(DISPLAY_DEVICE_FLAG.DISPLAY_DEVICE_MIRRORING_DRIVER))
					continue;

				if (!TryGetDisplayIndex(display.DeviceName, out byte displayIndex))
					continue;

				byte monitorIndex = 0;

				for (uint j = 0; EnumDisplayDevices(display.DeviceName, j, ref monitor, EDD_GET_DEVICE_INTERFACE_NAME); j++)
				{
					if (!monitor.StateFlags.HasFlag(DISPLAY_DEVICE_FLAG.DISPLAY_DEVICE_ACTIVE))
						continue;

					yield return (display, displayIndex, monitor, monitorIndex);

					monitorIndex++;
				}
			}
		}

		private static bool TryGetDisplayIndex(string device, out byte index)
		{
			const string displayPattern = @"DISPLAY(?<index>\d{1,2})\s*$";

			var match = Regex.Match(device, displayPattern);
			if (!match.Success)
			{
				index = 0;
				return false;
			}

			index = byte.Parse(match.Groups["index"].Value);
			return true;
		}

		private static string GetDeviceInstanceId(string deviceId)
		{
			// The typical format of device ID is as follows:
			// \\?\DISPLAY#<hardware-specific-ID>#<instance-specific-ID>#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}
			// \\?\ is extended-length path prefix.
			// DISPLAY indicates display device.
			// {e6f07b5f-ee97-4a90-b076-33f57bf4eaa7} means GUID_DEVINTERFACE_MONITOR.

			int index = deviceId.IndexOf("DISPLAY", StringComparison.Ordinal);
			if (index < 0)
				return null;

			var fields = deviceId.Substring(index).Split('#');
			if (fields.Length < 3)
				return null;

			return string.Join(@"\", fields.Take(3));
		}

		public static HandleItem[] GetMonitorHandles()
		{
			var handleItems = new List<HandleItem>();

			if (!EnumDisplayMonitors(
				IntPtr.Zero,
				IntPtr.Zero,
				MonitorEnum,
				IntPtr.Zero))
			{
				return Array.Empty<HandleItem>();
			}

			bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
			{
				var monitorInfo = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };

				if (!GetMonitorInfo(hMonitor, ref monitorInfo))
				{
					Debug.WriteLine($"Failed to get information on a display monitor.");
				}
				else if (TryGetDisplayIndex(monitorInfo.szDevice, out byte displayIndex))
				{
					handleItems.Add(new HandleItem(
						monitorHandle: hMonitor,
						displayIndex: displayIndex));
				}
				return true;
			}

			return handleItems.ToArray();
		}

		public static bool Rotate(string deviceInstanceId)
		{
			if (!TryGetDisplay(deviceInstanceId, out string displayName, out DEVMODE dm))
				return false;

			// Change orientation 90 degrees clockwise.
			var orientation = dm.dmDisplayOrientation switch
			{
				DMDO.DMDO_90 => DMDO.DMDO_DEFAULT,
				DMDO.DMDO_180 => DMDO.DMDO_90,
				DMDO.DMDO_270 => DMDO.DMDO_180,
				_ => DMDO.DMDO_270
			};

			return RotateDisplay(displayName, ref dm, orientation);
		}

		private static bool TryGetDisplay(string deviceInstanceId, out string displayName, out DEVMODE dm)
		{
			foreach (var (display, _, monitor, _) in EnumerateDevices())
			{
				if (!string.Equals(GetDeviceInstanceId(monitor.DeviceID), deviceInstanceId, StringComparison.OrdinalIgnoreCase))
					continue;

				displayName = display.DeviceName;
				dm = new DEVMODE { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };

				// EnumDisplaySettings function only works with display device.
				return EnumDisplaySettings(
					displayName,
					ENUM_CURRENT_SETTINGS,
					ref dm);
			}

			displayName = null;
			dm = default;
			return false;
		}

		private static bool RotateDisplay(string displayName, ref DEVMODE dm, DMDO orientation)
		{
			static bool IsLandscape(DMDO value) => (value == DMDO.DMDO_DEFAULT) || (value == DMDO.DMDO_180);

			if (IsLandscape(dm.dmDisplayOrientation) != IsLandscape(orientation))
			{
				// Swap width and height.
				(dm.dmPelsWidth, dm.dmPelsHeight) = (dm.dmPelsHeight, dm.dmPelsWidth);
				dm.dmFields = DM.DM_PELSWIDTH | DM.DM_PELSHEIGHT;
			}

			dm.dmDisplayOrientation = orientation;
			dm.dmFields |= DM.DM_DISPLAYORIENTATION;

			return (ChangeDisplaySettingsEx(
				displayName,
				ref dm,
				IntPtr.Zero,
				0, // Change the display dynamically.
				IntPtr.Zero) == DISP_CHANGE_SUCCESSFUL);
		}
	}
}