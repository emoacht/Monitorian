using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StartupAgency.Helper;

/// <summary>
/// OS version information
/// </summary>
internal static class OsVersion
{
	/// <summary>
	/// Whether OS is Windows 7 (6.1) or greater
	/// </summary>
	public static bool Is7OrGreater => IsEqualToOrGreaterThan(6, 1);

	/// <summary>
	/// Whether OS is Windows 8 (6.2) or greater
	/// </summary>
	public static bool Is8OrGreater => IsEqualToOrGreaterThan(6, 2);

	/// <summary>
	/// Whether OS is Windows 8.1 (6.3) or greater
	/// </summary>
	public static bool Is8Point1OrGreater => IsEqualToOrGreaterThan(6, 3);

	/// <summary>
	/// Whether OS is Windows 10 (10.0.10240) or greater
	/// </summary>
	public static bool Is10OrGreater => IsEqualToOrGreaterThan(10, 0, 10240);

	/// <summary>
	/// Whether OS is Windows 10 (10.0.14393) or greater
	/// </summary>
	public static bool Is10Build14393OrGreater => IsEqualToOrGreaterThan(10, 0, 14393);

	/// <summary>
	/// Whether OS is Windows 11 (10.0.22000) or greater
	/// </summary>
	public static bool Is11OrGreater => IsEqualToOrGreaterThan(10, 0, 22000);

	#region Cache

	private static readonly Dictionary<string, bool> _cache = [];
	private static readonly object _lock = new();

	private static bool IsEqualToOrGreaterThan(in int major, in int minor = 0, in int build = 0, [CallerMemberName] string propertyName = null)
	{
		lock (_lock)
		{
			if (!_cache.TryGetValue(propertyName, out bool value))
			{
				value = (new Version(major, minor, build) <= GetOsVersion());
				_cache[propertyName] = value; // Indexer is safer than Dictionary.Add method.
			}
			return value;
		}
	}

	[DllImport("Ntdll.dll")]
	private static extern int RtlGetVersion(out OSVERSIONINFO lpVersionInformation);

	[StructLayout(LayoutKind.Sequential)]
	private struct OSVERSIONINFO
	{
		public uint dwOSVersionInfoSize;
		public uint dwMajorVersion;
		public uint dwMinorVersion;
		public uint dwBuildNumber;
		public uint dwPlatformId;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szCSDVersion;
	}

	private static Version GetOsVersion()
	{
		return (RtlGetVersion(out OSVERSIONINFO info) == 0) // STATUS_SUCCESS
			? new Version((int)info.dwMajorVersion, (int)info.dwMinorVersion, (int)info.dwBuildNumber)
			: throw new InvalidOperationException("Failed to get OS version.");
	}

	#endregion
}
