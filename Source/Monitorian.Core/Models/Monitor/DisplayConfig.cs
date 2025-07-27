using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Connecting and Configuring Displays (CCD) Functions
/// </summary>
internal class DisplayConfig
{
	#region Win32

	[DllImport("User32.dll")]
	private static extern int GetDisplayConfigBufferSizes(
		uint flags, // QDC_ONLY_ACTIVE_PATHS
		out uint numPathArrayElements,
		out uint numModeInfoArrayElements);

	[DllImport("User32.dll")]
	private static extern int QueryDisplayConfig(
		uint flags, // QDC_ONLY_ACTIVE_PATHS
		ref uint numPathArrayElements,
		[Out] DISPLAYCONFIG_PATH_INFO[] pathInfoArray,
		ref uint numModeInfoArrayElements,
		[Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
		IntPtr currentTopologyId);

	[DllImport("User32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(
		ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

	[DllImport("User32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(
		ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

	[DllImport("User32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(
		ref DISPLAYCONFIG_SDR_WHITE_LEVEL requestPacket);

	[DllImport("User32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(
		ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2 requestPacket);

	[DllImport("User32.dll")]
	private static extern int DisplayConfigSetDeviceInfo(
		ref DISPLAYCONFIG_DEVICE_INFO_HEADER requestPacket);

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_PATH_INFO
	{
		public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
		public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
		public uint flags;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct DISPLAYCONFIG_MODE_INFO
	{
		[FieldOffset(0)]
		public DISPLAYCONFIG_MODE_INFO_TYPE infoType;

		[FieldOffset(4)]
		public uint id;

		[FieldOffset(8)]
		public LUID adapterId;

		[FieldOffset(16)]
		public DISPLAYCONFIG_TARGET_MODE targetMode;

		[FieldOffset(16)]
		public DISPLAYCONFIG_SOURCE_MODE sourceMode;

		[FieldOffset(16)]
		public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
		public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
		public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
		public ushort edidManufactureId;
		public ushort edidProductCodeId;
		public uint connectorInstance;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string monitorFriendlyDeviceName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string monitorDevicePath;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string viewGdiDeviceName;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_PATH_SOURCE_INFO
	{
		public LUID adapterId;
		public uint id;
		public uint modeInfoIdx;
		public uint statusFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_PATH_TARGET_INFO
	{
		public LUID adapterId;
		public uint id;
		public uint modeInfoIdx;
		public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
		public DISPLAYCONFIG_ROTATION rotation;
		public uint scaling;
		public DISPLAYCONFIG_RATIONAL refreshRate;
		public uint scanLineOrdering;

		[MarshalAs(UnmanagedType.Bool)]
		public bool targetAvailable;

		public uint statusFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct LUID
	{
		public uint LowPart;
		public int HighPart;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_TARGET_MODE
	{
		public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
	{
		public ulong pixelRate;
		public DISPLAYCONFIG_RATIONAL hSyncFreq;
		public DISPLAYCONFIG_RATIONAL vSyncFreq;
		public DISPLAYCONFIG_2DREGION activeSize;
		public DISPLAYCONFIG_2DREGION totalSize;
		public uint videoStandard;
		public uint scanLineOrdering;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_RATIONAL
	{
		public uint Numerator;
		public uint Denominator;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct DISPLAYCONFIG_2DREGION
	{
		public uint cx;
		public uint cy;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_SOURCE_MODE
	{
		public uint width;
		public uint height;
		public uint pixelFormat;
		public POINT position;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
	{
		public POINT PathSourceSize;
		public RECT DesktopImageRegion;
		public RECT DesktopImageClip;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
	{
		public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
		public uint size;
		public LUID adapterId;
		public uint id;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
	{
		public uint value;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_SDR_WHITE_LEVEL
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		/// <summary>
		/// The monitor's current SDR white level, specified as a multiplier of 80 nits, multiplied
		/// by 1000. E.g. a value of 1000 would indicate that the SDR white level is 80 nits, while
		/// a value of 2000 would indicate an SDR white level of 160 nits.
		/// </summary>
		public uint SDRWhiteLevel;
	}

	// Derived from wingdi.h of Windows SDK (10.0.26100.0)
	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		private uint value; // union bit fields

		public readonly bool advancedColorSupported => (value & 1) == 1;        // A type of advanced color is supported  
		public readonly bool advancedColorActive => (value & 2) == 2;           // A type of advanced color is active (see currentColorMode for the specific advanced color mode)
																				// reserved1
		public readonly bool advancedColorLimitedByPolicy => (value & 8) == 8;  // System/OS policy is limiting advanced color options (see currentColorMode for the current mode)
		public readonly bool highDynamicRangeSupported => (value & 16) == 16;   // HDR is supported
		public readonly bool highDynamicRangeUserEnabled => (value & 32) == 32; // HDR is enabled by the user (but may not be active)
		public readonly bool wideColorSupported => (value & 64) == 64;          // Wide color gamut is supported
		public readonly bool wideColorUserEnabled => (value & 128) == 128;      // Wide color gamut is enabled by the user (but may not be active)

		public DISPLAYCONFIG_COLOR_ENCODING colorEncoding;
		public uint bitsPerColorChannel;
		public DISPLAYCONFIG_ADVANCED_COLOR_MODE activeColorMode; // The active color mode for this monitor
	}

	// Undocumented
	[StructLayout(LayoutKind.Sequential)]
	private struct DISPLAYCONFIG_SET_SDR_WHITE_LEVEL
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		public uint SDRWhiteLevel;
		public byte flag;
	}

	private enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
	{
		DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
		DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
		DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
		DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
		DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
		DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6,
		DISPLAYCONFIG_DEVICE_INFO_GET_SUPPORT_VIRTUAL_RESOLUTION = 7,
		DISPLAYCONFIG_DEVICE_INFO_SET_SUPPORT_VIRTUAL_RESOLUTION = 8,
		DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9,
		DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10,
		DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL = 11,

		// Supplemented by wingdi.h of Windows SDK (10.0.22000.0)
		DISPLAYCONFIG_DEVICE_INFO_GET_MONITOR_SPECIALIZATION = 12,
		DISPLAYCONFIG_DEVICE_INFO_SET_MONITOR_SPECIALIZATION = 13,

		// Supplemented by wingdi.h of Windows SDK (10.0.26100.0)
		DISPLAYCONFIG_DEVICE_INFO_SET_RESERVED1 = 14,
		DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2 = 15,
		DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE = 16,
		DISPLAYCONFIG_DEVICE_INFO_SET_WCG_STATE = 17,

		DISPLAYCONFIG_DEVICE_INFO_SET_SDR_WHITE_LEVEL = 0xFFFFFFEE, // Undocumented
		DISPLAYCONFIG_DEVICE_INFO_FORCE_UINT32 = 0xFFFFFFFF
	}

	private enum DISPLAYCONFIG_COLOR_ENCODING : uint
	{
		DISPLAYCONFIG_COLOR_ENCODING_RGB = 0,
		DISPLAYCONFIG_COLOR_ENCODING_YCBCR444 = 1,
		DISPLAYCONFIG_COLOR_ENCODING_YCBCR422 = 2,
		DISPLAYCONFIG_COLOR_ENCODING_YCBCR420 = 3,
		DISPLAYCONFIG_COLOR_ENCODING_INTENSITY = 4,
		DISPLAYCONFIG_COLOR_ENCODING_FORCE_UINT32 = 0xFFFFFFFF
	}

	private enum DISPLAYCONFIG_ADVANCED_COLOR_MODE
	{
		DISPLAYCONFIG_ADVANCED_COLOR_MODE_SDR, // RGB888 composition, display-referred color, display-referred luminance
		DISPLAYCONFIG_ADVANCED_COLOR_MODE_WCG, // Advanced color (FP16 scRGB composition), scene-referred color, display-referred luminance
		DISPLAYCONFIG_ADVANCED_COLOR_MODE_HDR, // Advanced color (FP16 scRGB composition), scene-referred color, scene-referred luminance
	}

	private enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
	{
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER = 0xFFFFFFFF,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 = 0,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO = 1,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO = 2,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO = 3,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI = 4,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI = 5,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS = 6,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_D_JPN = 8,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDI = 9,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL = 10,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED = 11,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL = 12,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED = 13,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDTVDONGLE = 14,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST = 15,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_WIRED = 16,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_VIRTUAL = 17,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000000,
		DISPLAYCONFIG_OUTPUT_TECHNOLOGY_FORCE_UINT32 = 0xFFFFFFFF
	}

	private enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
	{
		DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1,
		DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2,
		DISPLAYCONFIG_MODE_INFO_TYPE_DESKTOP_IMAGE = 3,
		DISPLAYCONFIG_MODE_INFO_TYPE_FORCE_UINT32 = 0xFFFFFFFF
	}

	private enum DISPLAYCONFIG_ROTATION : uint
	{
		DISPLAYCONFIG_ROTATION_IDENTITY = 1,
		DISPLAYCONFIG_ROTATION_ROTATE90 = 2,
		DISPLAYCONFIG_ROTATION_ROTATE180 = 3,
		DISPLAYCONFIG_ROTATION_ROTATE270 = 4,
		DISPLAYCONFIG_ROTATION_FORCE_UINT32 = 0xFFFFFFFF
	}

	private const uint QDC_ONLY_ACTIVE_PATHS = 2;

	private const int ERROR_SUCCESS = 0;
	private const int ERROR_NOT_SUPPORTED = 0x32; // 50
	private const int ERROR_INVALID_PARAMETER = 0x57; // 87

	#endregion

	#region Type

	[DataContract]
	public class DisplayItem
	{
		[DataMember(Order = 0)]
		public string DeviceInstanceId { get; }

		[DataMember(Order = 1)]
		public string DisplayName { get; }

		[DataMember(Order = 2)]
		public bool IsInternal { get; }

		[DataMember(Order = 3)]
		public float RefreshRate { get; }

		[DataMember(Order = 4)]
		public string ConnectionDescription { get; }

		[DataMember(Order = 5)]
		public bool IsAvailable { get; }

		public DisplayIdSet DisplayIdSet { get; }

		public DisplayItem(
			string deviceInstanceId,
			string displayName,
			bool isInternal,
			float refreshRate,
			string connectionDescription,
			bool isAvailable,
			DisplayIdSet displayIdSet)
		{
			this.DeviceInstanceId = deviceInstanceId;
			this.DisplayName = displayName;
			this.IsInternal = isInternal;
			this.RefreshRate = refreshRate;
			this.ConnectionDescription = connectionDescription;
			this.IsAvailable = isAvailable;
			this.DisplayIdSet = displayIdSet;
		}
	}

	#endregion

	public static IEnumerable<DisplayItem> EnumerateDisplayConfigs()
	{
		if (GetDisplayConfigBufferSizes(
			QDC_ONLY_ACTIVE_PATHS,
			out uint pathCount,
			out uint modeCount) is not ERROR_SUCCESS)
			yield break;

		var displayPaths = new DISPLAYCONFIG_PATH_INFO[pathCount];
		var displayModes = new DISPLAYCONFIG_MODE_INFO[modeCount];

		if (QueryDisplayConfig(
			QDC_ONLY_ACTIVE_PATHS,
			ref pathCount,
			displayPaths,
			ref modeCount,
			displayModes,
			IntPtr.Zero) is not ERROR_SUCCESS)
			yield break;

		foreach (var displayPath in displayPaths)
		{
			var displayMode = displayModes
				.Where(x => x.infoType is DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
				.FirstOrDefault(x => x.id == displayPath.targetInfo.id);
			if (displayMode.Equals(default(DISPLAYCONFIG_MODE_INFO)))
				continue;

			var displayIdSet = new DisplayIdSet(displayMode.adapterId, displayMode.id);
			if (!TryGetDeviceName(displayIdSet, out var deviceName))
				continue;

			var deviceInstanceId = DeviceConversion.ConvertToDeviceInstanceId(deviceName.monitorDevicePath);

			yield return new DisplayItem(
				deviceInstanceId: deviceInstanceId,
				displayName: deviceName.monitorFriendlyDeviceName,
				isInternal: (deviceName.outputTechnology is DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL),
				refreshRate: displayPath.targetInfo.refreshRate.Numerator / (float)displayPath.targetInfo.refreshRate.Denominator,
				connectionDescription: GetConnectionDescription(deviceName.outputTechnology),
				isAvailable: displayPath.targetInfo.targetAvailable,
				displayIdSet: displayIdSet);
		}
	}

	private static bool TryGetDeviceName(DisplayIdSet displayIdSet, out DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName)
	{
		deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
		{
			header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
			{
				type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
				size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
				adapterId = displayIdSet.AdapterId,
				id = displayIdSet.Id
			}
		};

		int error = DisplayConfigGetDeviceInfo(ref deviceName);
		return (error is ERROR_SUCCESS);
	}

	/// <summary>
	/// Determines if HDR is set for a specified monitor (10.0.26100.0 or greater only).
	/// </summary>
	/// <param name="displayIdSet">Set of identifiers</param>
	/// <returns>True if successfully determines that HDR is set</returns>
	public static bool IsHdr(DisplayIdSet displayIdSet)
	{
		if (!OsVersion.Is11Build26100OrGreater)
			return false;

		var aci = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2
		{
			header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
			{
				type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2,
				size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2>(),
				adapterId = displayIdSet.AdapterId,
				id = displayIdSet.Id
			}
		};

		int error = DisplayConfigGetDeviceInfo(ref aci);
		if (error is not ERROR_SUCCESS)
			return false;

		//Debug.WriteLine($"advancedColorSupported: {aci.advancedColorSupported}");
		//Debug.WriteLine($"highDynamicRangeSupported: {aci.highDynamicRangeSupported}");
		//Debug.WriteLine($"highDynamicRangeUserEnabled: {aci.highDynamicRangeUserEnabled}");
		//Debug.WriteLine($"activeColorMode: {aci.activeColorMode}");

		return (aci.activeColorMode is DISPLAYCONFIG_ADVANCED_COLOR_MODE.DISPLAYCONFIG_ADVANCED_COLOR_MODE_HDR);
	}

	public static (AccessResult result, float sdrWhiteLevel) GetSdrWhiteLevel(DisplayIdSet displayIdSet)
	{
		var whiteLevel = new DISPLAYCONFIG_SDR_WHITE_LEVEL
		{
			header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
			{
				type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL,
				size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SDR_WHITE_LEVEL>(),
				adapterId = displayIdSet.AdapterId,
				id = displayIdSet.Id
			}
		};

		int error = DisplayConfigGetDeviceInfo(ref whiteLevel);
		return error switch
		{
			ERROR_SUCCESS => (result: AccessResult.Succeeded, (whiteLevel.SDRWhiteLevel / 1000F * 80F)),
			ERROR_NOT_SUPPORTED => (result: AccessResult.NotSupported, 0F),
			_ => (result: new AccessResult(AccessStatus.Failed, $"{nameof(GetSdrWhiteLevel)} Error: {error}"), 0F)
		};
	}

	public static AccessResult SetSdrWhiteLevel(DisplayIdSet displayIdSet, float sdrWhiteLevel)
	{
		var whiteLevel = new DISPLAYCONFIG_SET_SDR_WHITE_LEVEL
		{
			header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
			{
				type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_SET_SDR_WHITE_LEVEL,
				size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_SDR_WHITE_LEVEL>(),
				adapterId = displayIdSet.AdapterId,
				id = displayIdSet.Id
			},
			SDRWhiteLevel = (uint)Math.Round((sdrWhiteLevel / 80F * 1000F), MidpointRounding.AwayFromZero),
			flag = 1
		};

		int error = DisplayConfigSetDeviceInfo(ref whiteLevel.header);
		return error switch
		{
			ERROR_SUCCESS => AccessResult.Succeeded,
			ERROR_NOT_SUPPORTED => AccessResult.NotSupported,
			_ => new AccessResult(AccessStatus.Failed, $"{nameof(SetSdrWhiteLevel)} Error: {error}")
		};
	}

	private static string GetConnectionDescription(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology)
	{
		return outputTechnology switch
		{
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER => "Other",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 => "VGA",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_D_JPN => "AnalogTV",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI => "DVI",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI => "HDMI",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS => "LVDS",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDI => "SDI",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED => "DisplayPort",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED => "UDI",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDTVDONGLE => "SDTV",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST => "Miracast",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_WIRED => "Wired",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_VIRTUAL => "Virtual",
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL => "Internal",
			_ => null
		};
	}
}

/// <summary>
/// A set of identifiers for DisplayConfig functions
/// </summary>
internal class DisplayIdSet(DisplayConfig.LUID adapterId, uint id)
{
	/// <summary>
	/// DISPLAYCONFIG_MODE_INFO.adapterId & DISPLAYCONFIG_DEVICE_INFO_HEADER.adapterId
	/// </summary>
	/// <remarks>
	/// This corresponds to <see cref="Windows.Devices.Display.DisplayMonitor.DisplayAdapterId"/>.
	/// https://learn.microsoft.com/en-us/uwp/api/windows.devices.display.displaymonitor.displayadapterid
	/// </remarks>
	public DisplayConfig.LUID AdapterId => new() { LowPart = lowPart, HighPart = highPart };
	private readonly uint lowPart = adapterId.LowPart;
	private readonly int highPart = adapterId.HighPart;

	/// <summary>
	/// DISPLAYCONFIG_MODE_INFO.id & DISPLAYCONFIG_DEVICE_INFO_HEADER.id
	/// </summary>
	/// <remarks>
	/// This corresponds to <see cref="Windows.Devices.Display.DisplayMonitor.DisplayAdapterTargetId"/>.
	/// https://learn.microsoft.com/en-us/uwp/api/windows.devices.display.displaymonitor.displayadaptertargetid
	/// </remarks>
	public readonly uint Id = id;
}