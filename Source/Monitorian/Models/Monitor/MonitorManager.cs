using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Helper;
using UniversalSupplement;

namespace Monitorian.Models.Monitor
{
	internal class MonitorManager
	{
		#region Type

		private class DeviceItemPlus
		{
			private readonly DeviceContext.DeviceItem _deviceItem;

			public string DeviceInstanceId => _deviceItem.DeviceInstanceId;
			public string Description => _deviceItem.Description;
			public string AlternateDescription { get; }
			public byte DisplayIndex => _deviceItem.DisplayIndex;
			public byte MonitorIndex => _deviceItem.MonitorIndex;

			public DeviceItemPlus(
				DeviceContext.DeviceItem deviceItem,
				string alternateDescription = null)
			{
				this._deviceItem = deviceItem ?? throw new ArgumentNullException(nameof(deviceItem));
				this.AlternateDescription = alternateDescription ?? deviceItem.Description;
			}
		}

		#endregion

		public static async Task<IEnumerable<IMonitor>> EnumerateMonitorsAsync()
		{
			var deviceItems = await GetMonitorDevicesAsync();

			return EnumerateMonitors(deviceItems);
		}

		private static async Task<List<DeviceItemPlus>> GetMonitorDevicesAsync()
		{
			if (!OsVersion.Is10Redstone4OrNewer)
				return DeviceContext.EnumerateMonitorDevices().Select(x => new DeviceItemPlus(x)).ToList();

			const string genericDescription = "Generic PnP Monitor";

			var displayItems = await DisplayInformation.GetDisplayMonitorsAsync();

			return DeviceContext.EnumerateMonitorDevices()
				.Select(x =>
				{
					string alternateDescription = null;
					if (string.Equals(x.Description, genericDescription, StringComparison.OrdinalIgnoreCase))
					{
						var displayItem = displayItems.FirstOrDefault(y => string.Equals(x.DeviceInstanceId, y.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
						if (!string.IsNullOrWhiteSpace(displayItem?.DisplayName))
							alternateDescription = displayItem.DisplayName;
					}
					return new DeviceItemPlus(x, alternateDescription);
				})
				.ToList();
		}

		private static IEnumerable<IMonitor> EnumerateMonitors(List<DeviceItemPlus> deviceItems)
		{
			if (!(deviceItems?.Any() == true))
				yield break;

			// By DDC/CI
			foreach (var handleItem in DeviceContext.GetMonitorHandles())
			{
				foreach (var physicalItem in MonitorConfiguration.EnumeratePhysicalMonitors(handleItem.MonitorHandle))
				{
					int index = -1;
					if (physicalItem.IsBrightnessSupported)
					{
						index = deviceItems.FindIndex(x =>
							(x.DisplayIndex == handleItem.DisplayIndex) &&
							(x.MonitorIndex == physicalItem.MonitorIndex) &&
							string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
					}
					if (index < 0)
					{
						physicalItem.Handle.Dispose();
						continue;
					}

					var deviceItem = deviceItems[index];
					yield return new DdcMonitorItem(
						deviceInstanceId: deviceItem.DeviceInstanceId,
						description: deviceItem.AlternateDescription,
						displayIndex: deviceItem.DisplayIndex,
						monitorIndex: deviceItem.MonitorIndex,
						handle: physicalItem.Handle,
						isLowLevel: physicalItem.IsLowLevel);

					deviceItems.RemoveAt(index);
					if (deviceItems.Count == 0)
						yield break;
				}
			}

			// By WMI
			var installedItems = DeviceInstallation.EnumerateInstalledMonitors().ToArray();

			foreach (var desktopItem in MSMonitor.EnumerateDesktopMonitors())
			{
				foreach (var installedItem in installedItems)
				{
					int index = -1;
					if (desktopItem.BrightnessLevels.Any())
					{
						index = deviceItems.FindIndex(x =>
							string.Equals(x.DeviceInstanceId, desktopItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase) &&
							string.Equals(x.DeviceInstanceId, installedItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
					}
					if (index < 0)
						continue;

					var deviceItem = deviceItems[index];
					yield return new WmiMonitorItem(
						deviceInstanceId: deviceItem.DeviceInstanceId,
						description: deviceItem.AlternateDescription,
						displayIndex: deviceItem.DisplayIndex,
						monitorIndex: deviceItem.MonitorIndex,
						brightnessLevels: desktopItem.BrightnessLevels,
						isRemovable: installedItem.IsRemovable);

					deviceItems.RemoveAt(index);
					if (deviceItems.Count == 0)
						yield break;
				}
			}

			// Rest
			foreach (var deviceItem in deviceItems)
			{
				yield return new InaccessibleMonitorItem(
					deviceInstanceId: deviceItem.DeviceInstanceId,
					description: deviceItem.AlternateDescription,
					displayIndex: deviceItem.DisplayIndex,
					monitorIndex: deviceItem.MonitorIndex);
			}
		}

		#region Probe

		public static string ProbeMonitors()
		{
			var data = new MonitorData();

			using (var ms = new MemoryStream())
			using (var jw = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true))
			{
				var serializer = new DataContractJsonSerializer(typeof(MonitorData));
				serializer.WriteObject(jw, data);
				jw.Flush();
				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}

		[DataContract]
		private class MonitorData
		{
			[DataMember(Order = 0, Name = "Device Context - DeviceItems")]
			public DeviceContext.DeviceItem[] DeviceItems { get; private set; }

			[DataMember(Order = 1, Name = "Monitor Configuration - PhysicalItems")]
			public Dictionary<DeviceContext.HandleItem, MonitorConfiguration.PhysicalItem[]> PhysicalItems { get; private set; }

			[DataMember(Order = 2, Name = "Device Installation - InstalledItems")]
			public DeviceInstallation.InstalledItem[] InstalledItems { get; private set; }

			[DataMember(Order = 3, Name = "MSMonitorClass - DesktopItems")]
			public MSMonitor.DesktopItem[] DesktopItems { get; private set; }

			[DataMember(Order = 4, Name = "DisplayMonitor - DisplayItems")]
			public DisplayInformation.DisplayItem[] DisplayItems { get; private set; }

			public MonitorData()
			{
				DeviceItems = DeviceContext.EnumerateMonitorDevices().ToArray();

				PhysicalItems = DeviceContext.GetMonitorHandles().ToDictionary(
					x => x,
					x => MonitorConfiguration.EnumeratePhysicalMonitors(x.MonitorHandle).ToArray());

				InstalledItems = DeviceInstallation.EnumerateInstalledMonitors().ToArray();
				DesktopItems = MSMonitor.EnumerateDesktopMonitors().ToArray();

				if (OsVersion.Is10Redstone4OrNewer)
					DisplayItems = DisplayInformation.GetDisplayMonitorsAsync().Result;
			}
		}

		#endregion
	}
}