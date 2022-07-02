using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Views.Input.Touchpad
{
	internal static class TouchpadHelper
	{
		#region Win32

		[DllImport("User32", SetLastError = true)]
		private static extern uint GetRawInputDeviceList(
			[Out] RAWINPUTDEVICELIST[] pRawInputDeviceList,
			ref uint puiNumDevices,
			uint cbSize);

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTDEVICELIST
		{
			public IntPtr hDevice;
			public uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
		}

		private const uint RIM_TYPEMOUSE = 0;
		private const uint RIM_TYPEKEYBOARD = 1;
		private const uint RIM_TYPEHID = 2;

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool RegisterRawInputDevices(
			RAWINPUTDEVICE[] pRawInputDevices,
			uint uiNumDevices,
			uint cbSize);

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTDEVICE
		{
			public ushort usUsagePage;
			public ushort usUsage;
			public uint dwFlags; // RIDEV_REMOVE or RIDEV_INPUTSINK
			public IntPtr hwndTarget;
		}

		private const uint RIDEV_REMOVE = 0x00000001;
		private const uint RIDEV_INPUTSINK = 0x00000100;

		[DllImport("User32.dll", SetLastError = true)]
		private static extern uint GetRawInputData(
			IntPtr hRawInput, // lParam in WM_INPUT
			uint uiCommand, // RID_HEADER
			IntPtr pData,
			ref uint pcbSize,
			uint cbSizeHeader);

		private const uint RID_INPUT = 0x10000003;

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUT
		{
			public RAWINPUTHEADER Header;
			public RAWHID Hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTHEADER
		{
			public uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
			public uint dwSize;
			public IntPtr hDevice;
			public IntPtr wParam; // wParam in WM_INPUT
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWHID
		{
			public uint dwSizeHid;
			public uint dwCount;

			public IntPtr bRawData; // This is not for use.
		}

		[DllImport("User32.dll", SetLastError = true)]
		private static extern uint GetRawInputDeviceInfo(
			IntPtr hDevice, // hDevice by RAWINPUTHEADER
			uint uiCommand, // RIDI_PREPARSEDDATA
			IntPtr pData,
			ref uint pcbSize);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern uint GetRawInputDeviceInfo(
			IntPtr hDevice, // hDevice by RAWINPUTDEVICELIST
			uint uiCommand, // RIDI_DEVICEINFO
			ref RID_DEVICE_INFO pData,
			ref uint pcbSize);

		private const uint RIDI_PREPARSEDDATA = 0x20000005;
		private const uint RIDI_DEVICEINFO = 0x2000000b;

		[StructLayout(LayoutKind.Sequential)]
		private struct RID_DEVICE_INFO
		{
			public uint cbSize; // This is determined to accommodate RID_DEVICE_INFO_KEYBOARD.
			public uint dwType;
			public RID_DEVICE_INFO_HID hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RID_DEVICE_INFO_HID
		{
			public uint dwVendorId;
			public uint dwProductId;
			public uint dwVersionNumber;
			public ushort usUsagePage;
			public ushort usUsage;
		}

		[DllImport("Hid.dll", SetLastError = true)]
		private static extern uint HidP_GetCaps(
			IntPtr PreparsedData,
			out HIDP_CAPS Capabilities);

		private const uint HIDP_STATUS_SUCCESS = 0x00110000;

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_CAPS
		{
			public ushort Usage;
			public ushort UsagePage;
			public ushort InputReportByteLength;
			public ushort OutputReportByteLength;
			public ushort FeatureReportByteLength;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
			public ushort[] Reserved;

			public ushort NumberLinkCollectionNodes;
			public ushort NumberInputButtonCaps;
			public ushort NumberInputValueCaps;
			public ushort NumberInputDataIndices;
			public ushort NumberOutputButtonCaps;
			public ushort NumberOutputValueCaps;
			public ushort NumberOutputDataIndices;
			public ushort NumberFeatureButtonCaps;
			public ushort NumberFeatureValueCaps;
			public ushort NumberFeatureDataIndices;
		}

		[DllImport("Hid.dll", CharSet = CharSet.Auto)]
		private static extern uint HidP_GetValueCaps(
			HIDP_REPORT_TYPE ReportType,
			[Out] HIDP_VALUE_CAPS[] ValueCaps,
			ref ushort ValueCapsLength,
			IntPtr PreparsedData);

		private enum HIDP_REPORT_TYPE
		{
			HidP_Input,
			HidP_Output,
			HidP_Feature
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_VALUE_CAPS
		{
			public ushort UsagePage;
			public byte ReportID;

			[MarshalAs(UnmanagedType.U1)]
			public bool IsAlias;

			public ushort BitField;
			public ushort LinkCollection;
			public ushort LinkUsage;
			public ushort LinkUsagePage;

			[MarshalAs(UnmanagedType.U1)]
			public bool IsRange;
			[MarshalAs(UnmanagedType.U1)]
			public bool IsStringRange;
			[MarshalAs(UnmanagedType.U1)]
			public bool IsDesignatorRange;
			[MarshalAs(UnmanagedType.U1)]
			public bool IsAbsolute;
			[MarshalAs(UnmanagedType.U1)]
			public bool HasNull;

			public byte Reserved;
			public ushort BitSize;
			public ushort ReportCount;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public ushort[] Reserved2;

			public uint UnitsExp;
			public uint Units;
			public int LogicalMin;
			public int LogicalMax;
			public int PhysicalMin;
			public int PhysicalMax;

			// Range
			public ushort UsageMin;
			public ushort UsageMax;
			public ushort StringMin;
			public ushort StringMax;
			public ushort DesignatorMin;
			public ushort DesignatorMax;
			public ushort DataIndexMin;
			public ushort DataIndexMax;

			// NotRange
			public ushort Usage => UsageMin;
			// ushort Reserved1;
			public ushort StringIndex => StringMin;
			// ushort Reserved2;
			public ushort DesignatorIndex => DesignatorMin;
			// ushort Reserved3;
			public ushort DataIndex => DataIndexMin;
			// ushort Reserved4;
		}

		[DllImport("Hid.dll", CharSet = CharSet.Auto)]
		private static extern uint HidP_GetUsageValue(
			HIDP_REPORT_TYPE ReportType,
			ushort UsagePage,
			ushort LinkCollection,
			ushort Usage,
			out uint UsageValue,
			IntPtr PreparsedData,
			IntPtr Report,
			uint ReportLength);

		#endregion

		// Precision Touchpad (PTP) in HID Clients Supported in Windows
		// https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/hid-architecture#hid-clients-supported-in-windows
		private const ushort TouchpadUsagePage = 0x000D;
		private const ushort TouchpadUsage = 0x0005;

		public static bool Exists()
		{
			uint deviceListCount = 0;
			uint rawInputDeviceListSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();

			if (GetRawInputDeviceList(
				null,
				ref deviceListCount,
				rawInputDeviceListSize) != 0)
			{
				return false;
			}

			var devices = new RAWINPUTDEVICELIST[deviceListCount];

			if (GetRawInputDeviceList(
				devices,
				ref deviceListCount,
				rawInputDeviceListSize) != deviceListCount)
			{
				return false;
			}

			foreach (var device in devices.Where(x => x.dwType == RIM_TYPEHID))
			{
				uint deviceInfoSize = 0;

				if (GetRawInputDeviceInfo(
					device.hDevice,
					RIDI_DEVICEINFO,
					IntPtr.Zero,
					ref deviceInfoSize) != 0)
				{
					continue;
				}

				var deviceInfo = new RID_DEVICE_INFO { cbSize = deviceInfoSize };

				if (GetRawInputDeviceInfo(
					device.hDevice,
					RIDI_DEVICEINFO,
					ref deviceInfo,
					ref deviceInfoSize) == unchecked((uint)-1))
				{
					continue;
				}

				if ((deviceInfo.hid.usUsagePage == TouchpadUsagePage) &&
					(deviceInfo.hid.usUsage == TouchpadUsage))
				{
					return true;
				}
			}
			return false;
		}

		#region Register/Unregister

		public static bool RegisterInput(IntPtr windowHandle)
		{
			var device = new RAWINPUTDEVICE
			{
				usUsagePage = TouchpadUsagePage,
				usUsage = TouchpadUsage,
				dwFlags = 0, // WM_INPUT messages come only when the window is in the foreground.
				hwndTarget = windowHandle
			};

			return RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
		}

		public static bool UnregisterInput()
		{
			var device = new RAWINPUTDEVICE
			{
				usUsagePage = TouchpadUsagePage,
				usUsage = TouchpadUsage,
				dwFlags = RIDEV_REMOVE,
				hwndTarget = IntPtr.Zero
			};

			return RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
		}

		#endregion

		public const int WM_INPUT = 0x00FF;
		public const int RIM_INPUT = 0;
		public const int RIM_INPUTSINK = 1;

		public static TouchpadContact[] ParseInput(IntPtr lParam)
		{
			// Get RAWINPUT.
			uint rawInputSize = 0;
			uint rawInputHeaderSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();

			if (GetRawInputData(
				lParam,
				RID_INPUT,
				IntPtr.Zero,
				ref rawInputSize,
				rawInputHeaderSize) != 0)
			{
				return null;
			}

			RAWINPUT rawInput;
			byte[] rawHidRawData;

			IntPtr rawInputPointer = IntPtr.Zero;
			try
			{
				rawInputPointer = Marshal.AllocHGlobal((int)rawInputSize);

				if (GetRawInputData(
					lParam,
					RID_INPUT,
					rawInputPointer,
					ref rawInputSize,
					rawInputHeaderSize) != rawInputSize)
				{
					return null;
				}

				rawInput = Marshal.PtrToStructure<RAWINPUT>(rawInputPointer);

				var rawInputData = new byte[rawInputSize];
				Marshal.Copy(rawInputPointer, rawInputData, 0, rawInputData.Length);

				rawHidRawData = new byte[rawInput.Hid.dwSizeHid * rawInput.Hid.dwCount];
				int rawInputOffset = (int)rawInputSize - rawHidRawData.Length;
				Buffer.BlockCopy(rawInputData, rawInputOffset, rawHidRawData, 0, rawHidRawData.Length);
			}
			finally
			{
				Marshal.FreeHGlobal(rawInputPointer);
			}

			// Parse RAWINPUT.
			IntPtr preparsedDataPointer = IntPtr.Zero;
			IntPtr rawHidRawDataPointer = IntPtr.Zero;
			try
			{
				uint preparsedDataSize = 0;

				if (GetRawInputDeviceInfo(
					rawInput.Header.hDevice,
					RIDI_PREPARSEDDATA,
					IntPtr.Zero,
					ref preparsedDataSize) != 0)
				{
					return null;
				}

				preparsedDataPointer = Marshal.AllocHGlobal((int)preparsedDataSize);

				if (GetRawInputDeviceInfo(
					rawInput.Header.hDevice,
					RIDI_PREPARSEDDATA,
					preparsedDataPointer,
					ref preparsedDataSize) != preparsedDataSize)
				{
					return null;
				}

				if (HidP_GetCaps(
					preparsedDataPointer,
					out HIDP_CAPS caps) != HIDP_STATUS_SUCCESS)
				{
					return null;
				}

				ushort valueCapsLength = caps.NumberInputValueCaps;
				var valueCaps = new HIDP_VALUE_CAPS[valueCapsLength];

				if (HidP_GetValueCaps(
					HIDP_REPORT_TYPE.HidP_Input,
					valueCaps,
					ref valueCapsLength,
					preparsedDataPointer) != HIDP_STATUS_SUCCESS)
				{
					return null;
				}

				rawHidRawDataPointer = Marshal.AllocHGlobal(rawHidRawData.Length);
				Marshal.Copy(rawHidRawData, 0, rawHidRawDataPointer, rawHidRawData.Length);

				uint scanTime = 0;
				uint contactCount = 0;
				TouchpadContactCreator creator = new();
				List<TouchpadContact> contacts = new();

				foreach (var valueCap in valueCaps.OrderBy(x => x.LinkCollection))
				{
					if (HidP_GetUsageValue(
						HIDP_REPORT_TYPE.HidP_Input,
						valueCap.UsagePage,
						valueCap.LinkCollection,
						valueCap.Usage,
						out uint value,
						preparsedDataPointer,
						rawHidRawDataPointer,
						(uint)rawHidRawData.Length) != HIDP_STATUS_SUCCESS)
					{
						continue;
					}

					// Usage Page and ID in Windows Precision Touchpad input reports
					// https://docs.microsoft.com/en-us/windows-hardware/design/component-guidelines/windows-precision-touchpad-required-hid-top-level-collections#windows-precision-touchpad-input-reports
					switch (valueCap.LinkCollection)
					{
						case 0:
							switch (valueCap.UsagePage, valueCap.Usage)
							{
								case (0x0D, 0x56): // Scan Time
									scanTime = value;
									break;

								case (0x0D, 0x54): // Contact Count
									contactCount = value;
									break;
							}
							break;

						default:
							switch (valueCap.UsagePage, valueCap.Usage)
							{
								case (0x0D, 0x51): // Contact ID
									creator.ContactId = (int)value;
									break;

								case (0x01, 0x30): // X
									creator.X = (int)value;
									break;

								case (0x01, 0x31): // Y
									creator.Y = (int)value;
									break;
							}
							break;
					}

					if (creator.TryCreate(out TouchpadContact contact))
					{
						contacts.Add(contact);
						if (contacts.Count >= contactCount)
							break;

						creator.Clear();
					}
				}

				return contacts.ToArray();
			}
			finally
			{
				Marshal.FreeHGlobal(preparsedDataPointer);
				Marshal.FreeHGlobal(rawHidRawDataPointer);
			}
		}
	}
}