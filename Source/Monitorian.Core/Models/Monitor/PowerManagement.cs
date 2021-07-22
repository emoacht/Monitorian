﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

using Monitorian.Core.Models.Watcher;

namespace Monitorian.Core.Models.Monitor
{
	/// <summary>
	/// Power Management Functions
	/// </summary>
	internal class PowerManagement
	{
		#region Win32

		[DllImport("PowrProf.dll")]
		private static extern uint PowerGetActiveScheme(
			IntPtr UserRootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct)] out Guid activePolicyGuid);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerReadACValueIndex(
			IntPtr RootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid PowerSettingGuid,
			out uint AcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerReadDCValueIndex(
			IntPtr RootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid PowerSettingGuid,
			out uint DcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerWriteACValueIndex(
			IntPtr RootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid PowerSettingGuid,
			uint AcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerWriteDCValueIndex(
			IntPtr RootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct), In] Guid PowerSettingGuid,
			uint DcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerSetActiveScheme(
			IntPtr UserRootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct), In] Guid SchemeGuid);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetSystemPowerStatus(
			out SYSTEM_POWER_STATUS systemPowerStatus);

		[StructLayout(LayoutKind.Sequential)]
		private struct SYSTEM_POWER_STATUS
		{
			public byte ACLineStatus;
			public byte BatteryFlag;
			public byte BatteryLifePercent;
			public byte Reserved1;
			public int BatteryLifeTime;
			public int BatteryFullLifeTime;
		}

		private const uint ERROR_SUCCESS = 0;

		#endregion

		// Video settings derived from winnt.h
		private static readonly Guid VIDEO_SUBGROUP = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");
		private static readonly Guid VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS = new Guid("fbd9aa66-9553-4097-ba44-ed6e9d65eab8");
		private static readonly Guid DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS = new Guid("aded5e82-b909-4619-9949-f5d71dac0bcb");
		private static readonly Guid DEVICE_POWER_POLICY_VIDEO_DIM_BRIGHTNESS = new Guid("f1fbfde2-a960-4165-9f88-50667911ce96");

		// AC/DC power source derived from winnt.h
		private static readonly Guid ACDC_POWER_SOURCE = new Guid("5d3e9a59-e9d5-4b00-a6bd-ff34ff516548");

		public static Guid GetActiveScheme()
		{
			if (PowerGetActiveScheme(
				IntPtr.Zero,
				out Guid activePolicyGuid) != ERROR_SUCCESS)
			{
				Debug.WriteLine("Failed to get active scheme.");
				return default;
			}
			return activePolicyGuid;
		}

		#region Adaptive Brightness

		public static bool IsAdaptiveBrightnessEnabled
		{
			get => _isAdaptiveBrightnessEnabled ??= CanAdaptiveBrightnessEnabled && CheckAdaptiveBrightnessEnabled();
			private set => _isAdaptiveBrightnessEnabled = value;
		}
		public static bool? _isAdaptiveBrightnessEnabled;

		public static (IReadOnlyCollection<Guid>, Action<PowerSettingChangedEventArgs>) GetOnPowerSettingChanged()
		{
			if (!CanAdaptiveBrightnessEnabled)
				return default;

			var settingGuids = new[]
			{
				ACDC_POWER_SOURCE,
				VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS
			};
			return (settingGuids, (e) => IsAdaptiveBrightnessEnabled = CheckAdaptiveBrightnessEnabled(e));
		}

		private static bool CheckAdaptiveBrightnessEnabled(PowerSettingChangedEventArgs e = null)
		{
			if (e?.Guid == VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS)
			{
				// 0: Off
				// 1: On
				return (e.Data == 1);
			}
			return (IsActiveSchemeAdaptiveBrightnessEnabled() is true);
		}

		private static bool CanAdaptiveBrightnessEnabled => _canAdaptiveBrightnessEnabled ??= LightSensor.AmbientLightSensorExists && IsSettingAdaptiveBrightnessAdded();
		private static bool? _canAdaptiveBrightnessEnabled;

		private static bool IsSettingAdaptiveBrightnessAdded()
		{
			var name = $@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\{VIDEO_SUBGROUP}\{VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS}"; // HKLM

			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(name))
				{
					// 1: Remove
					// 2: Add
					return ((int)key.GetValue("Attributes") == 2);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to check if adaptive brightness setting is added" + Environment.NewLine
					+ ex);
				return false;
			}
		}

		public static bool? IsActiveSchemeAdaptiveBrightnessEnabled()
		{
			var isOnline = IsOnline();
			if (!isOnline.HasValue)
				return null;

			var schemeGuid = GetActiveScheme();
			uint valueIndex;

			if (isOnline.Value)
			{
				if (PowerReadACValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS,
					out valueIndex) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to read AC Adaptive Brightness.");
					return null;
				}
			}
			else
			{
				if (PowerReadDCValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS,
					out valueIndex) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to read DC Adaptive Brightness.");
					return null;
				}
			}
			return (valueIndex == 1U);
		}

		public static bool EnableActiveSchemeAdaptiveBrightness() => SetActiveSchemeAdaptiveBrightness(true);
		public static bool DisableActiveSchemeAdaptiveBrightness() => SetActiveSchemeAdaptiveBrightness(false);

		private static bool SetActiveSchemeAdaptiveBrightness(bool enable)
		{
			var isOnline = IsOnline();
			if (!isOnline.HasValue)
				return false;

			var schemeGuid = GetActiveScheme();
			uint valueIndex = enable ? 1U : 0U;

			if (isOnline.Value)
			{
				if (PowerWriteACValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS,
					valueIndex) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to write AC Adaptive Brightness.");
					return false;
				}
			}
			else
			{
				if (PowerWriteDCValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					VIDEO_ADAPTIVE_DISPLAY_BRIGHTNESS,
					valueIndex) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to write DC Adaptive Brightness.");
					return false;
				}
			}

			if (PowerSetActiveScheme(
				IntPtr.Zero,
				schemeGuid) != ERROR_SUCCESS)
			{
				Debug.WriteLine("Failed to set active scheme.");
				return false;
			}
			return true;
		}

		#endregion

		#region Brightness

		public static int GetActiveSchemeBrightness()
		{
			var isOnline = IsOnline();
			if (!isOnline.HasValue)
				return -1;

			var schemeGuid = GetActiveScheme();
			uint valueIndex;

			if (isOnline.Value)
			{
				if (PowerReadACValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS,
					out valueIndex) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to read AC Brightness.");
					return -1;
				}
			}
			else
			{
				if (PowerReadDCValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS,
					out valueIndex) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to read DC Brightness.");
					return -1;
				}
			}
			return (int)valueIndex;
		}

		public static bool SetActiveSchemeBrightness(int brightness)
		{
			if (brightness is < 0 or > 100)
				throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be within 0 to 100.");

			var isOnline = IsOnline();
			if (!isOnline.HasValue)
				return false;

			var schemeGuid = GetActiveScheme();

			if (isOnline.Value)
			{
				if (PowerWriteACValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS,
					(uint)brightness) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to write AC Brightness.");
					return false;
				}
			}
			else
			{
				if (PowerWriteDCValueIndex(
					IntPtr.Zero,
					schemeGuid,
					VIDEO_SUBGROUP,
					DEVICE_POWER_POLICY_VIDEO_BRIGHTNESS,
					(uint)brightness) != ERROR_SUCCESS)
				{
					Debug.WriteLine("Failed to write DC Brightness");
					return false;
				}
			}

			if (PowerSetActiveScheme(
				IntPtr.Zero,
				schemeGuid) != ERROR_SUCCESS)
			{
				Debug.WriteLine("Failed to set active scheme.");
				return false;
			}
			return true;
		}

		#endregion

		#region AC/DC Status

		public static bool? IsOnline()
		{
			if (GetSystemPowerStatus(out SYSTEM_POWER_STATUS systemPowerStatus))
			{
				switch (systemPowerStatus.ACLineStatus)
				{
					case 0: return false; // Offline
					case 1: return true; // Online
				}
			}
			return null;
		}

		#endregion
	}
}