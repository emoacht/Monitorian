using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StartupAgency.Helper
{
	/// <summary>
	/// OS version information
	/// </summary>
	internal static class OsVersion
	{
		/// <summary>
		/// Whether OS is Windows Vista or newer
		/// </summary>
		/// <remarks>Windows Vista = version 6.0</remarks>
		public static bool IsVistaOrNewer => IsEqualToOrNewer(6);

		/// <summary>
		/// Whether OS is Windows 8 or newer
		/// </summary>
		/// <remarks>Windows 8 = version 6.2</remarks>
		public static bool Is8OrNewer => IsEqualToOrNewer(6, 2);

		/// <summary>
		/// Whether OS is Windows 8.1 or newer
		/// </summary>
		/// <remarks>Windows 8.1 = version 6.3</remarks>
		public static bool Is81OrNewer => IsEqualToOrNewer(6, 3);

		/// <summary>
		/// Whether OS is Windows 10 (Threshold 1) or newer
		/// </summary>
		/// <remarks>Windows 10 (Threshold 1) = version 10.0.10240</remarks>
		public static bool Is10Threshold1OrNewer => IsEqualToOrNewer(10, 0, 10240);

		/// <summary>
		/// Whether OS is Windows 10 (Redstone 1) or newer
		/// </summary>
		/// <remarks>Windows 10 (Redstone 1) = version 10.0.14393</remarks>
		public static bool Is10Redstone1OrNewer => IsEqualToOrNewer(10, 0, 14393);

		/// <summary>
		/// Whether OS is Windows 10 (Redstone 3) or newer
		/// </summary>
		/// <remarks>Windows 10 (Redstone 3) = version 10.0.16299</remarks>
		public static bool Is10Redstone3OrNewer => IsEqualToOrNewer(10, 0, 16299);

		#region Cache

		private static readonly Dictionary<string, bool> _cache = new Dictionary<string, bool>();

		private static bool IsEqualToOrNewer(int major, int minor = 0, int build = 0, [CallerMemberName] string propertyName = null)
		{
			if (!_cache.TryGetValue(propertyName, out bool value))
			{
				value = (new Version(major, minor, build) <= GetOsVersion());
				_cache.Add(propertyName, value);
			}
			return value;
		}

		private static Version GetOsVersion()
		{
			var query = new SelectQuery("Win32_OperatingSystem", "OSType = 18"); // WINNT
			using (var searcher = new ManagementObjectSearcher(query))
			using (var results = searcher.Get())
			{
				foreach (ManagementObject result in results)
				{
					using (result)
					{
						var input = (string)result.GetPropertyValue("Version");
						if (Version.TryParse(input, out Version output))
							return output;
					}
				}
				throw new InvalidOperationException("Failed to get OS version.");
			}
		}

		#endregion
	}
}
