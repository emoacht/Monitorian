using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

using Monitorian.Helper;
using StartupBridge;

namespace Monitorian.Models
{
	internal class StartupService
	{
		private const string Argument = "/startup";

		/// <summary>
		/// Whether this instance is presumed to have started on sign in
		/// </summary>
		/// <param name="lastCloseTime">Last close time of this application</param>
		/// <returns>True if started on sign in</returns>
		public static bool IsStartedOnSignIn(DateTimeOffset lastCloseTime)
		{
			// First, check the command-line arguments.
			if (Environment.GetCommandLineArgs().Skip(1).Contains(Argument))
				return true;

			// Second, check if this assembly is packaged and if the startup task is enabled.
			if (!_isPackaged || !StartupTaskIsEnabled())
				return false;

			// Third, compare last close time with session start time.
			if ((default(DateTimeOffset) < lastCloseTime) && TryGetLogonSessionStartTime(out DateTimeOffset startTime))
			{
				return (lastCloseTime < startTime);
			}
			return false;
		}

		/// <summary>
		/// Whether this instance can be registered in startup
		/// </summary>
		/// <returns>True if can be registered</returns>
		public static bool CanRegister()
		{
			return _isPackaged
				? StartupTaskCanEnable()
				: true;
		}

		/// <summary>
		/// Whether this instance is registered in startup
		/// </summary>
		/// <returns>True if registered</returns>
		public static bool IsRegistered()
		{
			return _isPackaged
				? StartupTaskIsEnabled()
				: RegistryIsAdded();
		}

		/// <summary>
		/// Registers this instance to startup.
		/// </summary>
		public static void Register()
		{
			if (_isPackaged)
			{
				StartupTaskEnable();
			}
			else
			{
				RegistryAdd();
			}
		}

		/// <summary>
		/// Unregisters this instance from startup.
		/// </summary>
		public static void Unregister()
		{
			if (_isPackaged)
			{
				StartupTaskDisable();
			}
			else
			{
				RegistryRemove();
			}
		}

		#region Startup task (AppX)

		/// <summary>
		/// Whether this assembly is packaged in AppX package
		/// </summary>
		private static readonly bool _isPackaged = OsVersion.Is10Redstone1OrNewer && PlatformInfo.IsPackaged;

		/// <summary>
		/// Id of startup task
		/// </summary>
		/// <remarks>This Id must match that in AppxManifest.xml.</remarks>
		private const string TaskId = "MonitorianStartupTask";

		private static bool StartupTaskCanEnable() => StartupTaskBroker.CanEnable(TaskId);
		private static bool StartupTaskIsEnabled() => StartupTaskBroker.IsEnabled(TaskId);
		private static bool StartupTaskEnable() => StartupTaskBroker.Enable(TaskId);
		private static void StartupTaskDisable() => StartupTaskBroker.Disable(TaskId);

		#endregion

		#region Registry

		private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private static readonly string _path = $"{Assembly.GetExecutingAssembly().Location} {Argument}";

		private static bool RegistryIsAdded()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, false))
			{
				var existingValue = key.GetValue(ProductInfo.Title) as string;
				return string.Equals(existingValue, _path, StringComparison.OrdinalIgnoreCase);
			}
		}

		private static bool RegistryAdd()
		{
			if (RegistryIsAdded())
				return true;

			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				key.SetValue(ProductInfo.Title, _path, RegistryValueKind.String);
			}
			return true;
		}

		private static void RegistryRemove()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				if (!key.GetValueNames().Contains(ProductInfo.Title)) // The content of value doesn't matter.
					return;

				key.DeleteValue(ProductInfo.Title, false);
			}
		}

		#endregion

		#region Session

		private static bool TryGetLogonSessionStartTime(out DateTimeOffset startTime)
		{
			var query = new SelectQuery("Win32_LogonSession", "LogonType = 2");
			using (var searcher = new ManagementObjectSearcher(query))
			using (var sessions = searcher.Get())
			{
				foreach (ManagementObject session in sessions)
				{
					var buff = (string)session.GetPropertyValue("StartTime");
					if (string.IsNullOrEmpty(buff))
						continue;

					startTime = new DateTimeOffset(ManagementDateTimeConverter.ToDateTime(buff));
					return true;
				}
			}
			startTime = default(DateTimeOffset);
			return false;
		}

		#endregion
	}
}