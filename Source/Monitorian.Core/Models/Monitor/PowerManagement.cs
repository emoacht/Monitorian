using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
			[MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
			out uint AcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerReadDCValueIndex(
			IntPtr RootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
			out uint DcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerWriteACValueIndex(
			IntPtr RootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
			uint AcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerWriteDCValueIndex(
			IntPtr RootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
			uint DcValueIndex);

		[DllImport("PowrProf.dll")]
		private static extern uint PowerSetActiveScheme(
			IntPtr UserRootPowerKey, // Always null
			[MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid);

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

		private static readonly Guid SUB_VIDEO = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");
		private static readonly Guid ADAPTBRIGHT = new Guid("fbd9aa66-9553-4097-ba44-ed6e9d65eab8");
		private static readonly Guid VIDEO_BRIGHTNESS = new Guid("aded5e82-b909-4619-9949-f5d71dac0bcb");
		private static readonly Guid VIDEO_DIM_BRIGHTNESS = new Guid("f1fbfde2-a960-4165-9f88-50667911ce96");

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

		public static bool? IsActiveSchemeAdaptiveBrightnessEnabled()
		{
			var isOnline = IsOnline();
			if (!isOnline.HasValue)
				return null;

			var schemeGuid = GetActiveScheme();
			uint valueIndex = 0;

			if (isOnline.Value)
			{
				if (PowerReadACValueIndex(
					IntPtr.Zero,
					schemeGuid,
					SUB_VIDEO,
					ADAPTBRIGHT,
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
					SUB_VIDEO,
					ADAPTBRIGHT,
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
					SUB_VIDEO,
					ADAPTBRIGHT,
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
					SUB_VIDEO,
					ADAPTBRIGHT,
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
					SUB_VIDEO,
					VIDEO_BRIGHTNESS,
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
					SUB_VIDEO,
					VIDEO_BRIGHTNESS,
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
			if ((brightness < 0) || (100 < brightness))
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
					SUB_VIDEO,
					VIDEO_BRIGHTNESS,
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
					SUB_VIDEO,
					VIDEO_BRIGHTNESS,
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