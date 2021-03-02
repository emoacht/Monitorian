using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor
{
	/// <summary>
	/// Device Information Functions
	/// </summary>
	internal class DeviceInformation
	{
		#region Win32

		[DllImport("Setupapi.dll", SetLastError = true)]
		private static extern IntPtr SetupDiGetClassDevs(
			[MarshalAs(UnmanagedType.LPStruct), In] Guid ClassGuid,
			IntPtr Enumerator, // Null
			IntPtr hwndParent, // Null
			DIGCF Flags);

		private const int INVALID_HANDLE_VALUE = -1;
		private const int ERROR_NO_MORE_ITEMS = 259;

		[DllImport("Setupapi.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiDestroyDeviceInfoList(
			IntPtr DeviceInfoSet);

		[DllImport("Setupapi.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiEnumDeviceInfo(
			IntPtr DeviceInfoSet,
			uint MemberIndex,
			ref SP_DEVINFO_DATA DeviceInfoData);

		[DllImport("Setupapi.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiEnumDeviceInterfaces(
			IntPtr DeviceInfoSet,
			IntPtr DeviceInfoData,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid InterfaceClassGuid,
			uint MemberIndex,
			ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

		[DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiGetDeviceRegistryProperty(
			IntPtr DeviceInfoSet,
			ref SP_DEVINFO_DATA DeviceInfoData,
			SPDRP Property,
			out uint PropertyRegDataType,
			byte[] PropertyBuffer,
			uint PropertyBufferSize,
			out uint RequiredSize);

		[DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiGetDeviceInstanceId(
			IntPtr DeviceInfoSet,
			ref SP_DEVINFO_DATA DeviceInfoData,
			[Out] StringBuilder DeviceInstanceId,
			uint DeviceInstanceIdSize,
			out uint RequiredSize);

		[StructLayout(LayoutKind.Sequential)]
		private struct SP_DEVINFO_DATA
		{
			public uint cbSize;
			public Guid ClassGuid;
			public uint DevInst;
			public IntPtr Reserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SP_DEVICE_INTERFACE_DATA
		{
			public uint cbSize;
			public Guid InterfaceClassGuid;
			public SPINT Flags;
			public IntPtr Reserved;
		}

		[Flags]
		private enum DIGCF : uint
		{
			DIGCF_DEFAULT = 0x00000001, // only valid with DIGCF_DEVICEINTERFACE
			DIGCF_PRESENT = 0x00000002,
			DIGCF_ALLCLASSES = 0x00000004,
			DIGCF_PROFILE = 0x00000008,
			DIGCF_DEVICEINTERFACE = 0x00000010
		}

		[Flags]
		private enum SPINT : uint
		{
			SPINT_ACTIVE = 0x00000001,
			SPINT_DEFAULT = 0x00000002,
			SPINT_REMOVED = 0x00000004
		}

		private enum SPDRP : uint
		{
			SPDRP_DEVICEDESC = 0x00000000, // DeviceDesc (R/W)
			SPDRP_HARDWAREID = 0x00000001, // HardwareID (R/W)
			SPDRP_COMPATIBLEIDS = 0x00000002, // CompatibleIDs (R/W)
			SPDRP_UNUSED0 = 0x00000003, // unused
			SPDRP_SERVICE = 0x00000004, // Service (R/W)
			SPDRP_UNUSED1 = 0x00000005, // unused
			SPDRP_UNUSED2 = 0x00000006, // unused
			SPDRP_CLASS = 0x00000007, // Class (R--tied to ClassGUID)
			SPDRP_CLASSGUID = 0x00000008, // ClassGUID (R/W)
			SPDRP_DRIVER = 0x00000009, // Driver (R/W)
			SPDRP_CONFIGFLAGS = 0x0000000A, // ConfigFlags (R/W)
			SPDRP_MFG = 0x0000000B, // Mfg (R/W)
			SPDRP_FRIENDLYNAME = 0x0000000C, // FriendlyName (R/W)
			SPDRP_LOCATION_INFORMATION = 0x0000000D, // LocationInformation (R/W)
			SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E, // PhysicalDeviceObjectName (R)
			SPDRP_CAPABILITIES = 0x0000000F, // Capabilities (R)
			SPDRP_UI_NUMBER = 0x00000010, // UiNumber (R)
			SPDRP_UPPERFILTERS = 0x00000011, // UpperFilters (R/W)
			SPDRP_LOWERFILTERS = 0x00000012, // LowerFilters (R/W)
			SPDRP_BUSTYPEGUID = 0x00000013, // BusTypeGUID (R)
			SPDRP_LEGACYBUSTYPE = 0x00000014, // LegacyBusType (R)
			SPDRP_BUSNUMBER = 0x00000015, // BusNumber (R)
			SPDRP_ENUMERATOR_NAME = 0x00000016, // Enumerator Name (R)
			SPDRP_SECURITY = 0x00000017, // Security (R/W, binary form)
			SPDRP_SECURITY_SDS = 0x00000018, // Security (W, SDS form)
			SPDRP_DEVTYPE = 0x00000019, // Device Type (R/W)
			SPDRP_EXCLUSIVE = 0x0000001A, // Device is exclusive-access (R/W)
			SPDRP_CHARACTERISTICS = 0x0000001B, // Device Characteristics (R/W)
			SPDRP_ADDRESS = 0x0000001C, // Device Address (R)
			SPDRP_UI_NUMBER_DESC_FORMAT = 0x0000001D, // UiNumberDescFormat (R/W)
			SPDRP_DEVICE_POWER_DATA = 0x0000001E, // Device Power Data (R)
			SPDRP_REMOVAL_POLICY = 0x0000001F, // Removal Policy (R)
			SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020, // Hardware Removal Policy (R)
			SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021, // Removal Policy Override (RW)
			SPDRP_INSTALL_STATE = 0x00000022, // Device Install State (R)
			SPDRP_LOCATION_PATHS = 0x00000023, // Device Location Paths (R)
			SPDRP_BASE_CONTAINERID = 0x00000024, // Base ContainerID (R)
		}

		[Flags]
		private enum CM_DEVCAP : uint
		{
			CM_DEVCAP_LOCKSUPPORTED = 0x00000001,
			CM_DEVCAP_EJECTSUPPORTED = 0x00000002,
			CM_DEVCAP_REMOVABLE = 0x00000004,
			CM_DEVCAP_DOCKDEVICE = 0x00000008,
			CM_DEVCAP_UNIQUEID = 0x00000010,
			CM_DEVCAP_SILENTINSTALL = 0x00000020,
			CM_DEVCAP_RAWDEVICEOK = 0x00000040,
			CM_DEVCAP_SURPRISEREMOVALOK = 0x00000080,
			CM_DEVCAP_HARDWAREDISABLED = 0x00000100,
			CM_DEVCAP_NONDYNAMIC = 0x00000200
		}

		#endregion

		#region Type

		[DataContract]
		public class InstalledItem
		{
			[DataMember(Order = 0)]
			public string DeviceInstanceId { get; }

			[DataMember(Order = 1)]
			public string Description { get; }

			[DataMember(Order = 2)]
			public bool IsRemovable { get; }

			public InstalledItem(
				string deviceInstanceId,
				string description,
				bool isRemovable)
			{
				this.DeviceInstanceId = deviceInstanceId;
				this.Description = description;
				this.IsRemovable = isRemovable;
			}
		}

		#endregion

		private static readonly Guid GUID_DEVINTERFACE_MONITOR = new Guid("E6F07B5F-EE97-4a90-B076-33F57BF4EAA7");

		public static IEnumerable<InstalledItem> EnumerateInstalledMonitors()
		{
			var deviceInfoSet = IntPtr.Zero;
			try
			{
				deviceInfoSet = SetupDiGetClassDevs(
					GUID_DEVINTERFACE_MONITOR,
					IntPtr.Zero, // DISPLAY
					IntPtr.Zero,
					DIGCF.DIGCF_DEVICEINTERFACE | DIGCF.DIGCF_PRESENT);
				if (deviceInfoSet.ToInt32() == INVALID_HANDLE_VALUE) // Assuming 32bit process
				{
					Debug.WriteLine($"Failed to get device information list. {Error.GetMessage()}");
					yield break;
				}

				uint memberIndex = 0;

				while (true)
				{
					var deviceInfoData = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

					if (SetupDiEnumDeviceInfo(
						deviceInfoSet,
						memberIndex,
						ref deviceInfoData))
					{
						var deviceInstanceId = GetDeviceInstanceId(deviceInfoSet, deviceInfoData);
						var deviceDesc = GetDevicePropertyString(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_DEVICEDESC);

						var capability = (CM_DEVCAP)GetDevicePropertyUInt(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_CAPABILITIES);
						var isRemovable = capability.HasFlag(CM_DEVCAP.CM_DEVCAP_REMOVABLE);

						//Debug.WriteLine($"DeviceInstanceId: {deviceInstanceId}");
						//Debug.WriteLine($"DeviceDesc: {deviceDesc}");
						//Debug.WriteLine($"HardwareId: {GetDevicePropertyString(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_HARDWAREID)}");
						//Debug.WriteLine($"IsRemovable: {isRemovable}");

						yield return new InstalledItem(
							deviceInstanceId: deviceInstanceId,
							description: deviceDesc,
							isRemovable: isRemovable);
					}
					else
					{
						int errorCode = Marshal.GetLastWin32Error();
						if (errorCode == ERROR_NO_MORE_ITEMS)
							yield break;

						Debug.WriteLine($"Failed to enumerate device information structures. {Error.GetMessage(errorCode)}");
					}
					memberIndex++;
				}
			}
			finally
			{
				SetupDiDestroyDeviceInfoList(deviceInfoSet);
			}
		}

		private static string GetDeviceInstanceId(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
		{
			SetupDiGetDeviceInstanceId(
				DeviceInfoSet,
				ref DeviceInfoData,
				null,
				0,
				out uint requiredSize);

			var buffer = new StringBuilder((int)requiredSize);

			if (SetupDiGetDeviceInstanceId(
				DeviceInfoSet,
				ref DeviceInfoData,
				buffer,
				requiredSize,
				out _))
			{
				return buffer.ToString();
			}
			return string.Empty;
		}

		private static string GetDevicePropertyString(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData, SPDRP property)
		{
			var buffer = GetDevicePropertyBytes(DeviceInfoSet, DeviceInfoData, property);
			if (buffer is null)
				return string.Empty;

			return Encoding.Unicode.GetString(buffer).TrimEnd((char)0);
		}

		private static uint GetDevicePropertyUInt(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData, SPDRP property)
		{
			var buffer = GetDevicePropertyBytes(DeviceInfoSet, DeviceInfoData, property);
			if (buffer is null)
				return 0;

			return BitConverter.ToUInt32(buffer, 0);
		}

		private static byte[] GetDevicePropertyBytes(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData, SPDRP property)
		{
			SetupDiGetDeviceRegistryProperty(
				DeviceInfoSet,
				ref DeviceInfoData,
				property,
				out _,
				null,
				0,
				out uint requiredSize);

			var buffer = new byte[requiredSize];

			if (SetupDiGetDeviceRegistryProperty(
				DeviceInfoSet,
				ref DeviceInfoData,
				property,
				out _,
				buffer,
				requiredSize,
				out _))
			{
				return buffer;
			}
			return null;
		}
	}
}