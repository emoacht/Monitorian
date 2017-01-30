using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Models
{
	internal class RegistryService
	{
		public const string Argument = "/startup";

		private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";

		private static string _path = $"{Assembly.GetExecutingAssembly().Location} {Argument}";

		/// <summary>
		/// Whether this instance is registered in Run of HKCU.
		/// </summary>
		/// <returns>True if registered</returns>
		public static bool IsRegistered()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, false))
			{
				var existingValue = key.GetValue(ProductInfo.Title) as string;
				return string.Equals(existingValue, _path, StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Registers this instance to Run of HKCU.
		/// </summary>
		/// <returns>True if successfully registered</returns>
		public static bool Register()
		{
			if (IsRegistered())
				return false;

			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				key.SetValue(ProductInfo.Title, _path, RegistryValueKind.String);
			}
			return true;
		}

		/// <summary>
		/// Unregisters this instance from Run of HKCU.
		/// </summary>
		/// <returns>True if this instance successfully unregistered</returns>
		public static bool Unregister()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				if (!key.GetValueNames().Contains(ProductInfo.Title)) // The content of value doesn't matter.
					return false;

				key.DeleteValue(ProductInfo.Title, false);
			}
			return true;
		}
	}
}