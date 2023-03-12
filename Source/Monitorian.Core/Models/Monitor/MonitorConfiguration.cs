using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Monitor
{
	/// <summary>
	/// Monitor Configuration Functions
	/// </summary>
	internal class MonitorConfiguration
	{
		#region Win32

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
			IntPtr hMonitor,
			out uint pdwNumberOfPhysicalMonitors);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetPhysicalMonitorsFromHMONITOR(
			IntPtr hMonitor,
			uint dwPhysicalMonitorArraySize,
			[Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DestroyPhysicalMonitors(
			uint dwPhysicalMonitorArraySize,
			[In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyPhysicalMonitor(
			IntPtr hMonitor);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetMonitorCapabilities(
			SafePhysicalMonitorHandle hMonitor,
			out MC_CAPS pdwMonitorCapabilities,
			out MC_SUPPORTED_COLOR_TEMPERATURE pdwSupportedColorTemperatures);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetMonitorBrightness(
			SafePhysicalMonitorHandle hMonitor,
			out uint pdwMinimumBrightness,
			out uint pdwCurrentBrightness,
			out uint pdwMaximumBrightness);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetMonitorBrightness(
			SafePhysicalMonitorHandle hMonitor,
			uint dwNewBrightness);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCapabilitiesStringLength(
			SafePhysicalMonitorHandle hMonitor,
			out uint pdwCapabilitiesStringLengthInCharacters);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CapabilitiesRequestAndCapabilitiesReply(
			SafePhysicalMonitorHandle hMonitor,

			[MarshalAs(UnmanagedType.LPStr)]
			[Out] StringBuilder pszASCIICapabilitiesString,

			uint dwCapabilitiesStringLengthInCharacters);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CapabilitiesRequestAndCapabilitiesReply(
			SafePhysicalMonitorHandle hMonitor,
			IntPtr pszASCIICapabilitiesString,
			uint dwCapabilitiesStringLengthInCharacters);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetVCPFeatureAndVCPFeatureReply(
			SafePhysicalMonitorHandle hMonitor,
			byte bVCPCode,
			out LPMC_VCP_CODE_TYPE pvct,
			out uint pdwCurrentValue,
			out uint pdwMaximumValue);

		[DllImport("Dxva2.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetVCPFeature(
			SafePhysicalMonitorHandle hMonitor,
			byte bVCPCode,
			uint dwNewValue);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct PHYSICAL_MONITOR
		{
			public IntPtr hPhysicalMonitor;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szPhysicalMonitorDescription;
		}

		[Flags]
		private enum MC_CAPS
		{
			MC_CAPS_NONE = 0x00000000,
			MC_CAPS_MONITOR_TECHNOLOGY_TYPE = 0x00000001,
			MC_CAPS_BRIGHTNESS = 0x00000002,
			MC_CAPS_CONTRAST = 0x00000004,
			MC_CAPS_COLOR_TEMPERATURE = 0x00000008,
			MC_CAPS_RED_GREEN_BLUE_GAIN = 0x00000010,
			MC_CAPS_RED_GREEN_BLUE_DRIVE = 0x00000020,
			MC_CAPS_DEGAUSS = 0x00000040,
			MC_CAPS_DISPLAY_AREA_POSITION = 0x00000080,
			MC_CAPS_DISPLAY_AREA_SIZE = 0x00000100,
			MC_CAPS_RESTORE_FACTORY_DEFAULTS = 0x00000400,
			MC_CAPS_RESTORE_FACTORY_COLOR_DEFAULTS = 0x00000800,
			MC_RESTORE_FACTORY_DEFAULTS_ENABLES_MONITOR_SETTINGS = 0x00001000
		}

		[Flags]
		private enum MC_SUPPORTED_COLOR_TEMPERATURE
		{
			MC_SUPPORTED_COLOR_TEMPERATURE_NONE = 0x00000000,
			MC_SUPPORTED_COLOR_TEMPERATURE_4000K = 0x00000001,
			MC_SUPPORTED_COLOR_TEMPERATURE_5000K = 0x00000002,
			MC_SUPPORTED_COLOR_TEMPERATURE_6500K = 0x00000004,
			MC_SUPPORTED_COLOR_TEMPERATURE_7500K = 0x00000008,
			MC_SUPPORTED_COLOR_TEMPERATURE_8200K = 0x00000010,
			MC_SUPPORTED_COLOR_TEMPERATURE_9300K = 0x00000020,
			MC_SUPPORTED_COLOR_TEMPERATURE_10000K = 0x00000040,
			MC_SUPPORTED_COLOR_TEMPERATURE_11500K = 0x00000080
		}

		private enum LPMC_VCP_CODE_TYPE
		{
			MC_MOMENTARY,
			MC_SET_PARAMETER
		}

		#endregion

		#region Type

		[DataContract]
		public class PhysicalItem
		{
			[DataMember(Order = 0)]
			public string Description { get; }

			[DataMember(Order = 1)]
			public int MonitorIndex { get; }

			public SafePhysicalMonitorHandle Handle { get; }

			[DataMember(Order = 2)]
			public MonitorCapability Capability { get; }

			public PhysicalItem(
				string description,
				int monitorIndex,
				SafePhysicalMonitorHandle handle,
				MonitorCapability capability)
			{
				this.Description = description;
				this.MonitorIndex = monitorIndex;
				this.Handle = handle;
				this.Capability = capability;
			}
		}

		#endregion

		private enum VcpCode : byte
		{
			None = 0x0,
			Luminance = 0x10,
			Contrast = 0x12,
			Temperature = 0x14,
			SpeakerVolume = 0x62,
			PowerMode = 0xD6,
		}

		public static IEnumerable<PhysicalItem> EnumeratePhysicalMonitors(IntPtr monitorHandle, bool verbose = false)
		{
			if (!GetNumberOfPhysicalMonitorsFromHMONITOR(
				monitorHandle,
				out uint count))
			{
				Debug.WriteLine($"Failed to get the number of physical monitors. {Error.GetMessage()}");
				yield break;
			}
			if (count == 0)
			{
				yield break;
			}

			var physicalMonitors = new PHYSICAL_MONITOR[count];

			try
			{
				if (!GetPhysicalMonitorsFromHMONITOR(
					monitorHandle,
					count,
					physicalMonitors))
				{
					Debug.WriteLine($"Failed to get an array of physical monitors. {Error.GetMessage()}");
					yield break;
				}

				int monitorIndex = 0;

				foreach (var physicalMonitor in physicalMonitors)
				{
					var handle = new SafePhysicalMonitorHandle(physicalMonitor.hPhysicalMonitor);

					//Debug.WriteLine($"Description: {physicalMonitor.szPhysicalMonitorDescription}");
					//Debug.WriteLine($"Handle: {physicalMonitor.hPhysicalMonitor}");

					yield return new PhysicalItem(
						description: physicalMonitor.szPhysicalMonitorDescription,
						monitorIndex: monitorIndex,
						handle: handle,
						capability: GetMonitorCapability(handle, verbose));

					monitorIndex++;
				}
			}
			finally
			{
				// The physical monitor handles should be destroyed at a later stage.
			}
		}

		private static MonitorCapability GetMonitorCapability(SafePhysicalMonitorHandle physicalMonitorHandle, bool verbose)
		{
			bool isHighLevelSupported = GetMonitorCapabilities(
				physicalMonitorHandle,
				out MC_CAPS caps,
				out _)
				&& caps.HasFlag(MC_CAPS.MC_CAPS_BRIGHTNESS);

			if (GetCapabilitiesStringLength(
				physicalMonitorHandle,
				out uint capabilitiesStringLength))
			{
				var buffer = new StringBuilder((int)capabilitiesStringLength);

				if (CapabilitiesRequestAndCapabilitiesReply(
					physicalMonitorHandle,
					buffer,
					capabilitiesStringLength))
				{
					var capabilitiesString = buffer.ToString();
					IReadOnlyDictionary<byte, byte[]> vcpCodeValues = GetVcpCodeValues(capabilitiesString);

					return new MonitorCapability(
						isHighLevelBrightnessSupported: isHighLevelSupported,
						isLowLevelBrightnessSupported: vcpCodeValues.ContainsKey((byte)VcpCode.Luminance),
						isContrastSupported: vcpCodeValues.ContainsKey((byte)VcpCode.Contrast),
						temperatures: (vcpCodeValues.TryGetValue((byte)VcpCode.Temperature, out byte[] values) ? values : null),
						capabilitiesString: (verbose ? capabilitiesString : null),
						capabilitiesReport: (verbose ? MakeCapabilitiesReport(vcpCodeValues) : null),
						capabilitiesData: (verbose && !vcpCodeValues.Any() ? GetCapabilitiesData(physicalMonitorHandle, capabilitiesStringLength) : null));
				}
			}
			return new MonitorCapability(
				isHighLevelBrightnessSupported: isHighLevelSupported,
				isLowLevelBrightnessSupported: false,
				isContrastSupported: false);

			static string MakeCapabilitiesReport(IReadOnlyDictionary<byte, byte[]> vcpCodeValues)
			{
				var temperatureString = vcpCodeValues.TryGetValue((byte)VcpCode.Temperature, out byte[] values)
					&& (values is { Length: > 0 })
						? $"{true} ({string.Join(" ", values)})"
						: false.ToString();

				return $"Luminance: {vcpCodeValues.ContainsKey((byte)VcpCode.Luminance)}, " +
					   $"Contrast: {vcpCodeValues.ContainsKey((byte)VcpCode.Contrast)}, " +
					   $"Color Temperature: {temperatureString}, " +
					   $"Speaker Volume: {vcpCodeValues.ContainsKey((byte)VcpCode.SpeakerVolume)}, " +
					   $"Power Mode: {vcpCodeValues.ContainsKey((byte)VcpCode.PowerMode)}";
			}

			static byte[] GetCapabilitiesData(SafePhysicalMonitorHandle physicalMonitorHandle, uint capabilitiesStringLength)
			{
				var dataPointer = IntPtr.Zero;
				try
				{
					dataPointer = Marshal.AllocHGlobal((int)capabilitiesStringLength);

					if (CapabilitiesRequestAndCapabilitiesReply(
						physicalMonitorHandle,
						dataPointer,
						capabilitiesStringLength))
					{
						var data = new byte[capabilitiesStringLength];
						Marshal.Copy(dataPointer, data, 0, data.Length);
						return data;
					}
					return null;
				}
				finally
				{
					Marshal.FreeHGlobal(dataPointer);
				}
			}
		}

		private static IEnumerable<byte> EnumerateVcpCodes(string source)
		{
			if (string.IsNullOrEmpty(source))
				yield break;

			int index = source.IndexOf("vcp", StringComparison.Ordinal);
			if (index < 0)
				yield break;

			int depth = 0;
			var buffer = new StringBuilder(2);

			foreach (char c in source.Skip(index + 3))
			{
				if (!IsAscii(c))
					yield break;

				switch (c)
				{
					case '(':
						depth++;
						break;
					case ')':
						depth--;
						if (depth < 1)
						{
							yield break; // End of enumeration
						}
						break;
					default:
						if (depth is 1)
						{
							if (IsHexNumber(c))
							{
								buffer.Append(c);
								if (buffer.Length is 1)
									continue;
							}
							if (0 < buffer.Length)
							{
								yield return byte.Parse(buffer.ToString(), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
								buffer.Clear();
							}
						}
						break;
				}
			}

			static bool IsAscii(char c) => c <= 0x7F;
			static bool IsHexNumber(char c) => c is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f');
		}

		private static Dictionary<byte, byte[]> GetVcpCodeValues(string source)
		{
			var dic = new Dictionary<byte, byte[]>();

			if (string.IsNullOrEmpty(source))
				return dic;

			int index = source.IndexOf("vcp", StringComparison.Ordinal);
			if (index < 0)
				return dic;

			int depth = 0;
			var buffer1 = new StringBuilder(2);
			byte? lastKey = null;
			var buffer2 = new StringBuilder(2);
			var values = new List<byte>();

			foreach (char c in source.Skip(index + 3))
			{
				if (!IsAscii(c))
					break;

				switch (c)
				{
					case '(':
						depth++;
						break;
					case ')':
						depth--;
						switch (depth)
						{
							case < 1:
								goto end; // End of enumeration
							case 1:
								if (values.Any() && lastKey.HasValue)
								{
									dic[lastKey.Value] = values.ToArray();
									values.Clear();
								}
								break;
						}
						break;
					default:
						switch (depth)
						{
							case 1:
								if (IsHexNumber(c))
								{
									buffer1.Append(c);
									if (buffer1.Length is 1)
										continue;
								}
								if (0 < buffer1.Length)
								{
									lastKey = byte.Parse(buffer1.ToString(), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
									buffer1.Clear();
									dic[lastKey.Value] = null;
								}
								break;
							case 2:
								if (IsHexNumber(c))
								{
									buffer2.Append(c);
									if (buffer2.Length is 1)
										continue;
								}
								if (0 < buffer2.Length)
								{
									var value = byte.Parse(buffer2.ToString(), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
									buffer2.Clear();
									values.Add(value);
								}
								break;
						}
						break;
				}
			}
		end:
			return dic;

			static bool IsAscii(char c) => c <= 0x7F;
			static bool IsHexNumber(char c) => c is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f');
		}

		/// <summary>
		/// Gets raw brightnesses not represented in percentage.
		/// </summary>
		/// <param name="physicalMonitorHandle">Physical monitor handle</param>
		/// <param name="isHighLevelBrightnessSupported">Whether high level function is supported</param>
		/// <returns>
		/// <para>result: Result</para>
		/// <para>minimum: Raw minimum brightness (not always 0)</para>
		/// <para>current: Raw current brightness (not always 0 to 100)</para>
		/// <para>maximum: Raw maximum brightness (not always 100)</para>
		/// </returns>
		/// <remarks>
		/// Raw minimum and maximum brightnesses will become meaningful when they are not standard
		/// values (0 and 100) and so raw current brightness needs to be converted to brightness
		/// in percentage using those values. They are used to convert brightness in percentage
		/// back to raw brightness when settings brightness as well.
		/// </remarks>
		public static (AccessResult result, uint minimum, uint current, uint maximum) GetBrightness(SafePhysicalMonitorHandle physicalMonitorHandle, bool isHighLevelBrightnessSupported = true)
		{
			if (!isHighLevelBrightnessSupported)
				return GetVcpValue(physicalMonitorHandle, VcpCode.Luminance);

			if (!EnsurePhysicalMonitorHandle(physicalMonitorHandle))
				return (result: AccessResult.Failed, 0, 0, 0);

			if (GetMonitorBrightness(
				physicalMonitorHandle,
				out uint minimumBrightness,
				out uint currentBrightness,
				out uint maximumBrightness))
			{
				return (result: AccessResult.Succeeded,
					minimum: minimumBrightness,
					current: currentBrightness,
					maximum: maximumBrightness);
			}
			var (errorCode, message) = Error.GetCodeMessage();
			Debug.WriteLine($"Failed to get brightnesses. {message}");
			return (result: new AccessResult(GetStatus(errorCode), $"High level, {message}"), 0, 0, 0);
		}

		/// <summary>
		/// Gets raw contrast not represented in percentage.
		/// </summary>
		/// <param name="physicalMonitorHandle">Physical monitor handle</param>
		/// <returns>
		/// <para>result: Result</para>
		/// <para>minimum: Raw minimum contrast (0)</para>
		/// <para>current: Raw current contrast (not always 0 to 100)</para>
		/// <para>maximum: Raw maximum contrast (not always 100)</para>
		/// </returns>
		public static (AccessResult result, uint minimum, uint current, uint maximum) GetContrast(SafePhysicalMonitorHandle physicalMonitorHandle)
		{
			return GetVcpValue(physicalMonitorHandle, VcpCode.Contrast);
		}

		/// <summary>
		/// Gets raw color temperature.
		/// </summary>
		/// <param name="physicalMonitorHandle">Physical monitor handle</param>
		/// <returns>
		/// <para>result: Result</para>
		/// <para>current: Raw current color temperature</para>
		/// </returns>
		public static (AccessResult result, byte current) GetTemperature(SafePhysicalMonitorHandle physicalMonitorHandle)
		{
			var (result, _, current, _) = GetVcpValue(physicalMonitorHandle, VcpCode.Temperature);
			return (result, (byte)current);
		}

		private static (AccessResult result, uint minimum, uint current, uint maximum) GetVcpValue(SafePhysicalMonitorHandle physicalMonitorHandle, VcpCode vcpCode)
		{
			if (!EnsurePhysicalMonitorHandle(physicalMonitorHandle))
				return (result: AccessResult.Failed, 0, 0, 0);

			var status = AccessStatus.None;
			while (true)
			{
				if (GetVCPFeatureAndVCPFeatureReply(
					physicalMonitorHandle,
					(byte)vcpCode,
					out _,
					out uint currentValue,
					out uint maximumValue))
				{
					return (result: AccessResult.Succeeded,
						minimum: 0,
						current: currentValue,
						maximum: maximumValue);
				}
				var (errorCode, message) = Error.GetCodeMessage();
				Debug.WriteLine($"Failed to get VCP value ({vcpCode}). {message}");

				if (CheckPossibleTransientStatus(status, status = GetStatus(errorCode)))
					continue;

				return (result: new AccessResult(status, $"Low level, {message}"), 0, 0, 0);
			}
		}

		/// <summary>
		/// Sets raw brightness not represented in percentage.
		/// </summary>
		/// <param name="physicalMonitorHandle">Physical monitor handle</param>
		/// <param name="brightness">Raw brightness (not always 0 to 100)</param>
		/// <param name="isHighLevelBrightnessSupported">Whether high level function is supported</param>
		/// <returns>Result</returns>
		public static AccessResult SetBrightness(SafePhysicalMonitorHandle physicalMonitorHandle, uint brightness, bool isHighLevelBrightnessSupported = true)
		{
			if (!isHighLevelBrightnessSupported)
				return SetVcpValue(physicalMonitorHandle, VcpCode.Luminance, brightness);

			if (!EnsurePhysicalMonitorHandle(physicalMonitorHandle))
				return AccessResult.Failed;

			// SetMonitorBrightness function may return true even when it actually failed.
			if (SetMonitorBrightness(
				physicalMonitorHandle,
				brightness))
			{
				return AccessResult.Succeeded;
			}
			var (errorCode, message) = Error.GetCodeMessage();
			Debug.WriteLine($"Failed to set brightness. {message}");
			return new AccessResult(GetStatus(errorCode), $"High level {message}");
		}

		/// <summary>
		/// Sets raw contrast not represented in percentage.
		/// </summary>
		/// <param name="physicalMonitorHandle">Physical monitor handle</param>
		/// <param name="contrast">Raw contrast (not always 0 to 100)</param>
		/// <returns>Result</returns>
		public static AccessResult SetContrast(SafePhysicalMonitorHandle physicalMonitorHandle, uint contrast)
		{
			return SetVcpValue(physicalMonitorHandle, VcpCode.Contrast, contrast);
		}

		/// <summary>
		/// Sets raw color temperature.
		/// </summary>
		/// <param name="physicalMonitorHandle">Physical monitor handle</param>
		/// <param name="temperature">Raw color temperature</param>
		/// <returns></returns>
		public static AccessResult SetTemperature(SafePhysicalMonitorHandle physicalMonitorHandle, byte temperature)
		{
			return SetVcpValue(physicalMonitorHandle, VcpCode.Temperature, temperature);
		}

		private static AccessResult SetVcpValue(SafePhysicalMonitorHandle physicalMonitorHandle, VcpCode vcpCode, uint value)
		{
			if (!EnsurePhysicalMonitorHandle(physicalMonitorHandle))
				return AccessResult.Failed;

			var status = AccessStatus.None;
			while (true)
			{
				if (SetVCPFeature(
					physicalMonitorHandle,
					(byte)vcpCode,
					value))
				{
					return AccessResult.Succeeded;
				}
				var (errorCode, message) = Error.GetCodeMessage();
				Debug.WriteLine($"Failed to set VCP value ({vcpCode}). {message}");

				if (CheckPossibleTransientStatus(status, status = GetStatus(errorCode)))
					continue;

				return new AccessResult(status, $"Low level, {message}");
			}
		}

		private static bool EnsurePhysicalMonitorHandle(SafePhysicalMonitorHandle physicalMonitorHandle)
		{
			if (physicalMonitorHandle is null)
				throw new ArgumentNullException(nameof(physicalMonitorHandle));

			if (physicalMonitorHandle.IsClosed)
			{
				Debug.WriteLine("The physical monitor handle has been closed.");
				return false;
			}
			return true;
		}

		private static bool CheckPossibleTransientStatus(AccessStatus oldStatus, AccessStatus newStatus)
		{
			return (oldStatus == AccessStatus.None)
				&& (newStatus == AccessStatus.TransmissionFailed);
		}

		#region Error

		// Derived from winerror.h
		private const uint ERROR_GRAPHICS_DDCCI_VCP_NOT_SUPPORTED = 0xC0262584;
		private const uint ERROR_GRAPHICS_DDCCI_INVALID_DATA = 0xC0262585;
		private const uint ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_COMMAND = 0xC0262589;
		private const uint ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_LENGTH = 0xC026258A;
		private const uint ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_CHECKSUM = 0xC026258B;
		private const uint ERROR_GRAPHICS_I2C_ERROR_TRANSMITTING_DATA = 0xC0262582;
		private const uint ERROR_GRAPHICS_MONITOR_NO_LONGER_EXISTS = 0xC026258D;

		private static AccessStatus GetStatus(int errorCode)
		{
			return unchecked((uint)errorCode) switch
			{
				ERROR_GRAPHICS_DDCCI_VCP_NOT_SUPPORTED or
				ERROR_GRAPHICS_DDCCI_INVALID_DATA or
				ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_COMMAND or
				ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_LENGTH or
				ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_CHECKSUM => AccessStatus.DdcFailed,
				ERROR_GRAPHICS_I2C_ERROR_TRANSMITTING_DATA => AccessStatus.TransmissionFailed,
				ERROR_GRAPHICS_MONITOR_NO_LONGER_EXISTS => AccessStatus.NoLongerExist,
				_ => AccessStatus.Failed
			};
		}

		#endregion
	}

	[DataContract]
	internal class MonitorCapability
	{
		public bool IsBrightnessSupported => IsHighLevelBrightnessSupported || IsLowLevelBrightnessSupported;

		[DataMember(Order = 0)]
		public bool IsHighLevelBrightnessSupported { get; }

		[DataMember(Order = 1)]
		public bool IsLowLevelBrightnessSupported { get; }

		[DataMember(Order = 2)]
		public bool IsContrastSupported { get; }

		[DataMember(Order = 3)]
		public bool IsPrecleared { get; }

		/// <summary>
		/// Supported color temperatures
		/// </summary>
		/// <remarks>
		/// The following temperatures are defined.
		///  3:  4000° K
		///  4:  5000° K
		///  5:  6500° K
		///  6:  7500° K
		///  7:  8200° K
		///  8:  9300° K
		///  9: 10000° K
		/// 10: 11500° K
		/// Not all temperatures are supported in practice.
		/// An additional temperature can be inserted depending on a specific model.
		/// </remarks>
		public IReadOnlyList<byte> Temperatures { get; }

		public bool IsTemperatureSupported => (Temperatures is { Count: > 0 });
		[DataMember(Order = 4, Name = nameof(IsTemperatureSupported))]
		private string _isTemperatureSupportedString;

		[DataMember(Order = 5)]
		public string CapabilitiesString { get; }

		[DataMember(Order = 6)]
		public string CapabilitiesReport { get; }

		[DataMember(Order = 7)]
		public string CapabilitiesData { get; }

		[OnSerializing]
		private void OnSerializing(StreamingContext context)
		{
			_isTemperatureSupportedString = IsTemperatureSupported
				? $"true ({string.Join(" ", Temperatures)})"
				: "false";
		}

		public MonitorCapability(
			bool isHighLevelBrightnessSupported,
			bool isLowLevelBrightnessSupported,
			bool isContrastSupported,
			IReadOnlyList<byte> temperatures = null,
			string capabilitiesString = null,
			string capabilitiesReport = null,
			byte[] capabilitiesData = null) : this(
				isHighLevelBrightnessSupported: isHighLevelBrightnessSupported,
				isLowLevelBrightnessSupported: isLowLevelBrightnessSupported,
				isContrastSupported: isContrastSupported,
				isPrecleared: false,
				temperatures: temperatures,
				capabilitiesString: capabilitiesString,
				capabilitiesReport: capabilitiesReport,
				capabilitiesData: capabilitiesData)
		{ }

		private MonitorCapability(
			bool isHighLevelBrightnessSupported,
			bool isLowLevelBrightnessSupported,
			bool isContrastSupported,
			bool isPrecleared,
			IReadOnlyList<byte> temperatures,
			string capabilitiesString,
			string capabilitiesReport,
			byte[] capabilitiesData)
		{
			this.IsHighLevelBrightnessSupported = isHighLevelBrightnessSupported;
			this.IsLowLevelBrightnessSupported = isLowLevelBrightnessSupported;
			this.IsContrastSupported = isContrastSupported;
			this.IsPrecleared = isPrecleared;
			this.Temperatures = temperatures?.Clip<byte>(3, 10).ToArray(); // 3 is warmest and 10 is coldest.
			this.CapabilitiesString = capabilitiesString;
			this.CapabilitiesReport = capabilitiesReport;
			this.CapabilitiesData = (capabilitiesData is not null) ? Convert.ToBase64String(capabilitiesData) : null;
		}

		public static MonitorCapability PreclearedCapability => _preclearedCapability.Value;
		private static readonly Lazy<MonitorCapability> _preclearedCapability = new(() =>
			new(isHighLevelBrightnessSupported: false,
				isLowLevelBrightnessSupported: true,
				isContrastSupported: true,
				isPrecleared: true,
				temperatures: null,
				capabilitiesString: null,
				capabilitiesReport: null,
				capabilitiesData: null));
	}
}