using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
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
			ref RECT lprcMonitor,
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

		private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

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
					var deviceInstanceId = GetDeviceInstanceId(monitor.DeviceID);
					var isActive = monitor.StateFlags.HasFlag(DISPLAY_DEVICE_FLAG.DISPLAY_DEVICE_ACTIVE);

					//Debug.WriteLine($"DeviceId: {monitor.DeviceID}");
					//Debug.WriteLine($"DeviceInstanceId: {deviceInstanceId}");
					//Debug.WriteLine($"DeviceString: {monitor.DeviceString}");
					//Debug.WriteLine($"IsActive {isActive}");

					if (!isActive)
						continue;

					yield return new DeviceItem(
						deviceInstanceId: deviceInstanceId,
						description: monitor.DeviceString,
						displayIndex: displayIndex,
						monitorIndex: monitorIndex);

					monitorIndex++;
				}
			}
		}

		private static readonly Regex _displayPattern = new Regex(@"DISPLAY(?<index>\d{1,2})$", RegexOptions.Compiled);

		private static bool TryGetDisplayIndex(string device, out byte index)
		{
			var match = _displayPattern.Match(device.Trim());
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
			var index = deviceId.IndexOf("DISPLAY", StringComparison.Ordinal);
			if (index < 0)
				return null;

			var fields = deviceId.Substring(index).Split('#');
			if (fields.Length < 3)
				return null;

			return string.Join(@"\", fields.Take(3));
		}

		private static readonly List<HandleItem> _handleItems = new List<HandleItem>();

		public static HandleItem[] GetMonitorHandles()
		{
			try
			{
				if (!EnumDisplayMonitors(
					IntPtr.Zero,
					IntPtr.Zero,
					MonitorEnum,
					IntPtr.Zero))
				{
					Debug.WriteLine("Failed to enumerate display monitors.");
				}

				return _handleItems.ToArray();
			}
			finally
			{
				_handleItems.Clear();
			}
		}

		private static bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
		{
			var monitorInfo = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };

			if (!GetMonitorInfo(hMonitor, ref monitorInfo))
			{
				Debug.WriteLine($"Failed to get information on a display monitor.");
			}
			else
			{
				if (TryGetDisplayIndex(monitorInfo.szDevice, out byte displayIndex))
				{
					_handleItems.Add(new HandleItem(
						monitorHandle: hMonitor,
						displayIndex: displayIndex));
				}
			}
			return true;
		}
	}
}