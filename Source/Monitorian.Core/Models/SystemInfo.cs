using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Core.Models
{
	/// <summary>
	/// System information
	/// </summary>
	public static class SystemInfo
	{
		private class SystemInfoInternal
		{
			public string Manufacturer { get; }
			public string Model { get; }

			public SystemInfoInternal()
			{
				try
				{
					using var @class = new ManagementClass("Win32_ComputerSystem");
					using var instance = @class.GetInstances().Cast<ManagementObject>().FirstOrDefault();

					Manufacturer = instance?["Manufacturer"] as string;
					Model = instance?["Model"] as string;
					return;
				}
				catch (ManagementException)
				{
					Debug.WriteLine($"Failed to get instances by Win32_ComputerSystem");
				}

				const string keyName = @"SYSTEM\CurrentControlSet\Control\SystemInformation"; // HKLM

				using var key = Registry.LocalMachine.OpenSubKey(keyName);

				Manufacturer = key?.GetValue("SystemManufacturer") as string;
				Model = key?.GetValue("SystemProductName") as string;
			}
		}

		private static readonly Lazy<SystemInfoInternal> _instance = new(() => new());

		public static string Manufacturer => _instance.Value.Manufacturer;
		public static string Model => _instance.Value.Model;
	}
}