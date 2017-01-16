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
		public const string Arguments = "/startup";

		private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";

		private static string _path = $"{Assembly.GetExecutingAssembly().Location} {Arguments}";

		public static bool IsRegistered()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, false))
			{
				var existingValue = key.GetValue(ProductInfo.Title) as string;
				return string.Equals(existingValue, _path, StringComparison.OrdinalIgnoreCase);
			}
		}

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