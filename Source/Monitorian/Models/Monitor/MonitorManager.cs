using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
{
	public class MonitorManager
	{
		public static IEnumerable<IMonitor> EnumerateMonitors()
		{
			var deviceItems = DeviceContext.EnumerateMonitorDevices().ToList();
			if (deviceItems.Count == 0)
				yield break;

			//Debug.WriteLine("=== DDC/CI ===");

			foreach (var handleItem in DeviceContext.GetMonitorHandles())
			{
				foreach (var physicalItem in MonitorConfiguration.EnumeratePhysicalMonitors(handleItem.MonitorHandle))
				{
					//Debug.WriteLine($"Display: {handleItem.DisplayIndex}, Monitor: {physicalItem.MonitorIndex}");

					var index = deviceItems.FindIndex(x =>
						(x.DisplayIndex == handleItem.DisplayIndex) &&
						(x.MonitorIndex == physicalItem.MonitorIndex) &&
						string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
					if (0 <= index)
					{
						yield return new DdcMonitorItem(
							description: deviceItems[index].Description,
							deviceInstanceId: deviceItems[index].DeviceInstanceId,
							displayIndex: deviceItems[index].DisplayIndex,
							monitorIndex: deviceItems[index].MonitorIndex,
							handle: physicalItem.Handle);

						deviceItems.RemoveAt(index);
						if (deviceItems.Count == 0)
							yield break;
						else
							continue;
					}
					physicalItem.Handle.Dispose();
				}
			}

			//Debug.WriteLine("=== WMI ===");

			var installedItems = DeviceInstallation.EnumerateInstalledMonitors().ToArray();

			foreach (var desktopItem in MSMonitor.EnumerateDesktopMonitors())
			{
				foreach (var installedItem in installedItems)
				{
					var index = deviceItems.FindIndex(x =>
						string.Equals(x.DeviceInstanceId, desktopItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase) &&
						string.Equals(x.DeviceInstanceId, installedItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
					if (0 <= index)
					{
						yield return new WmiMonitorItem(
							description: deviceItems[index].Description,
							deviceInstanceId: deviceItems[index].DeviceInstanceId,
							displayIndex: deviceItems[index].DisplayIndex,
							monitorIndex: deviceItems[index].MonitorIndex,
							brightnessLevels: desktopItem.BrightnessLevels,
							isRemovable: installedItem.IsRemovable);

						deviceItems.RemoveAt(index);
						if (deviceItems.Count == 0)
							yield break;
					}
				}
			}
		}
	}
}