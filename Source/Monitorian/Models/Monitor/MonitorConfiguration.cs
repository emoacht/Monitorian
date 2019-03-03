﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
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

		private const byte LuminanceCode = 0x10; // VCP Code of Luminance

		#endregion

		#region Type

		[DataContract]
		public class PhysicalItem
		{
			[DataMember(Order = 0)]
			public string Description { get; private set; }

			[DataMember(Order = 1)]
			public int MonitorIndex { get; private set; }

			public SafePhysicalMonitorHandle Handle { get; }

			public bool IsSupported => IsHighLevelSupported || IsLowLevelSupported;

			[DataMember(Order = 2)]
			public bool IsHighLevelSupported { get; private set; }

			[DataMember(Order = 3)]
			public bool IsLowLevelSupported { get; private set; }

			[DataMember(Order = 4)]
			public string Capabilities { get; private set; }

			public PhysicalItem(
				string description,
				int monitorIndex,
				SafePhysicalMonitorHandle handle,
				bool isHighLevelSupported,
				bool isLowLevelSupported = false,
				string capabilities = null)
			{
				this.Description = description;
				this.MonitorIndex = monitorIndex;
				this.Handle = handle;
				this.IsHighLevelSupported = isHighLevelSupported;
				this.IsLowLevelSupported = isLowLevelSupported;
				this.Capabilities = capabilities;
			}
		}

		#endregion

		public static IEnumerable<PhysicalItem> EnumeratePhysicalMonitors(IntPtr monitorHandle, bool verbose = false)
		{
			if (!GetNumberOfPhysicalMonitorsFromHMONITOR(
				monitorHandle,
				out uint count))
			{
				Debug.WriteLine($"Failed to get the number of physical monitors. {Error.CreateMessage()}");
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
					Debug.WriteLine($"Failed to get an array of physical monitors. {Error.CreateMessage()}");
					yield break;
				}

				int monitorIndex = 0;

				foreach (var physicalMonitor in physicalMonitors)
				{
					var handle = new SafePhysicalMonitorHandle(physicalMonitor.hPhysicalMonitor);

					bool isHighLevelSupported = GetMonitorCapabilities(
						handle,
						out MC_CAPS caps,
						out MC_SUPPORTED_COLOR_TEMPERATURE _)
						&& caps.HasFlag(MC_CAPS.MC_CAPS_BRIGHTNESS);

					bool isLowLevelSupported = false;
					string capabilities = null;

					if (!isHighLevelSupported || verbose)
					{
						if (GetCapabilitiesStringLength(
							handle,
							out uint capabilitiesStringLength))
						{
							var capabilitiesString = new StringBuilder((int)capabilitiesStringLength);

							if (CapabilitiesRequestAndCapabilitiesReply(
								handle,
								capabilitiesString,
								capabilitiesStringLength))
							{
								capabilities = capabilitiesString.ToString();
								isLowLevelSupported = IsLowLevelSupported(capabilities);
							}
						}
					}

					//Debug.WriteLine($"Description: {physicalMonitor.szPhysicalMonitorDescription}");
					//Debug.WriteLine($"Handle: {physicalMonitor.hPhysicalMonitor}");
					//Debug.WriteLine($"IsHighLevelSupported: {isHighLevelSupported}");
					//Debug.WriteLine($"IsLowLevelSupported: {isLowLevelSupported}");
					//Debug.WriteLine($"Capabilities: {capabilities}");

					yield return new PhysicalItem(
						description: physicalMonitor.szPhysicalMonitorDescription,
						monitorIndex: monitorIndex,
						handle: handle,
						isHighLevelSupported: isHighLevelSupported,
						isLowLevelSupported: isLowLevelSupported,
						capabilities: verbose ? capabilities : null);

					monitorIndex++;
				}
			}
			finally
			{
				// The physical monitor handles should be destroyed at a later stage.
			}
		}

		private static bool IsLowLevelSupported(string source)
		{
			if (string.IsNullOrWhiteSpace(source))
				return false;

			var index = source.IndexOf("vcp", StringComparison.OrdinalIgnoreCase);
			if (index < 0)
				return false;

			return EnumerateCodes().Any(x => x == "10"); // 10 (hex) is VCP Code of Luminance.

			IEnumerable<string> EnumerateCodes()
			{
				int depth = 0;
				var buffer = new StringBuilder(2);

				foreach (char c in source.Skip(index + 3))
				{
					switch (c)
					{
						case '(':
							depth++;
							break;
						case ')':
							depth--;
							if (depth <= 0)
							{
								if (0 < buffer.Length)
								{
									yield return buffer.ToString();
								}
								yield break; // End of enumeration
							}
							break;
						default:
							if (depth == 1)
							{
								if (char.IsWhiteSpace(c))
								{
									if (buffer.Length < 1)
										continue;
								}
								else
								{
									buffer.Append(c);
									if (buffer.Length < 2)
										continue;
								}

								yield return buffer.ToString();
								buffer.Clear();
							}
							break;
					}
				}
			}
		}

		public static int GetBrightness(SafePhysicalMonitorHandle physicalMonitorHandle, bool useLowLevel = false)
		{
			if (physicalMonitorHandle is null)
				throw new ArgumentNullException(nameof(physicalMonitorHandle));

			if (physicalMonitorHandle.IsClosed)
			{
				Debug.WriteLine("Failed to get brightness. The physical monitor handle has been closed.");
				return -1;
			}

			if (!useLowLevel)
			{
				if (!GetMonitorBrightness(
					physicalMonitorHandle,
					out uint minimumBrightness,
					out uint currentBrightness,
					out uint maximumBrightness))
				{
					Debug.WriteLine($"Failed to get brightness. {Error.CreateMessage()}");
					return -1;
				}
				return (int)((float)(currentBrightness - minimumBrightness) / (maximumBrightness - minimumBrightness) * 100.0f + 0.5f);
			}
			else
			{
				if (!GetVCPFeatureAndVCPFeatureReply(
					physicalMonitorHandle,
					LuminanceCode,
					out LPMC_VCP_CODE_TYPE _,
					out uint currentValue,
					out uint maximumValue))
				{
					Debug.WriteLine($"Failed to get brightness (Low level). {Error.CreateMessage()}");
					return -1;
				}
				return (int)currentValue;
			}
		}

		public static bool SetBrightness(SafePhysicalMonitorHandle physicalMonitorHandle, int brightness, bool useLowLevel = false)
		{
			if (physicalMonitorHandle is null)
				throw new ArgumentNullException(nameof(physicalMonitorHandle));
			if ((brightness < 0) || (100 < brightness))
				throw new ArgumentOutOfRangeException(nameof(brightness), $"{nameof(brightness)} must be in the range of 0 to 100.");

			if (physicalMonitorHandle.IsClosed)
			{
				Debug.WriteLine("Failed to set brightness. The physical monitor handle has been closed.");
				return false;
			}

			if (!useLowLevel)
			{
				if (!SetMonitorBrightness(
					physicalMonitorHandle,
					(uint)brightness))
				{
					Debug.WriteLine($"Failed to set brightness. {Error.CreateMessage()}");
					return false;
				}
			}
			else
			{
				if (!SetVCPFeature(
					physicalMonitorHandle,
					LuminanceCode,
					(uint)brightness))
				{
					Debug.WriteLine($"Failed to set brightness (Low level). {Error.CreateMessage()}");
					return false;
				}
			}
			return true;
		}
	}
}