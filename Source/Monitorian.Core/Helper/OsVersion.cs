using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Helper
{
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
		/// Whether OS is Windows 10 (10.0.17134) or greater
		/// </summary>
		public static bool Is10Build17134OrGreater => IsEqualToOrGreaterThan(10, 0, 17134);

		/// <summary>
		/// Whether OS is Windows 11 (10.0.22000) or greater
		/// </summary>
		public static bool Is11OrGreater => IsEqualToOrGreaterThan(10, 0, 22000);

		/// <summary>
		/// Whether OS is Windows 11 (10.0.22621) or greater
		/// </summary>
		public static bool Is11Build22621OrGreater => IsEqualToOrGreaterThan(10, 0, 22621);

		#region Cache

		private static readonly Dictionary<string, bool> _cache = new();
		private static readonly object _lock = new();

		private static bool IsEqualToOrGreaterThan(in int major, in int minor = 0, in int build = 0, [CallerMemberName] string propertyName = null)
		{
			lock (_lock)
			{
				if (!_cache.TryGetValue(propertyName, out bool value))
				{
					value = (new Version(major, minor, build) <= Environment.OSVersion.Version);
					_cache[propertyName] = value; // Indexer is safer than Dictionary.Add method.
				}
				return value;
			}
		}

		#endregion
	}
}