using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Helper
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

		#region Cache

		private static readonly Dictionary<string, bool> _cache = new Dictionary<string, bool>();

		private static bool IsEqualToOrNewer(int major, int minor = 0, int build = 0, [CallerMemberName] string propertyName = null)
		{
			bool value;
			if (!_cache.TryGetValue(propertyName, out value))
			{
				value = new Version(major, minor, build) <= Environment.OSVersion.Version;
				_cache.Add(propertyName, value);
			}
			return value;
		}

		#endregion
	}
}