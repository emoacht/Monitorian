using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
{
	internal class MonitorManager
	{
		public static IEnumerable<IMonitor> EnumerateMonitors()
		{
			var deviceItems = DeviceContext.EnumerateMonitorDevices().ToList();
			if (deviceItems.Count == 0)
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
						description: deviceItem.Description,
						deviceInstanceId: deviceItem.DeviceInstanceId,
						displayIndex: deviceItem.DisplayIndex,
						monitorIndex: deviceItem.MonitorIndex,
						handle: physicalItem.Handle);

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
						description: deviceItem.Description,
						deviceInstanceId: deviceItem.DeviceInstanceId,
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
					description: deviceItem.Description,
					deviceInstanceId: deviceItem.DeviceInstanceId,
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

			public MonitorData()
			{
				DeviceItems = DeviceContext.EnumerateMonitorDevices().ToArray();

				PhysicalItems = DeviceContext.GetMonitorHandles().ToDictionary(
					x => x,
					x => MonitorConfiguration.EnumeratePhysicalMonitors(x.MonitorHandle).ToArray());

				InstalledItems = DeviceInstallation.EnumerateInstalledMonitors().ToArray();
				DesktopItems = MSMonitor.EnumerateDesktopMonitors().ToArray();
			}
		}

		#endregion
	}
}