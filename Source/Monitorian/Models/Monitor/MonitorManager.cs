using System;
using System.Collections.Generic;
using System.Linq;
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
					var index = deviceItems.FindIndex(x =>
						(x.DisplayIndex == handleItem.DisplayIndex) &&
						(x.MonitorIndex == physicalItem.MonitorIndex) &&
						string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
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
					var index = deviceItems.FindIndex(x =>
						string.Equals(x.DeviceInstanceId, desktopItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase) &&
						string.Equals(x.DeviceInstanceId, installedItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
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
	}
}