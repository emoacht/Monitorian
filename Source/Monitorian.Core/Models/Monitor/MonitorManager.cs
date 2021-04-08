using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Monitor
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
			public bool IsInternal { get; }

			public DeviceItemPlus(
				DeviceContext.DeviceItem deviceItem,
				string alternateDescription = null,
				bool isInternal = true)
			{
				this._deviceItem = deviceItem ?? throw new ArgumentNullException(nameof(deviceItem));
				this.AlternateDescription = alternateDescription ?? deviceItem.Description;
				this.IsInternal = isInternal;
			}
		}

		#endregion

		public static async Task<IEnumerable<IMonitor>> EnumerateMonitorsAsync()
		{
			var deviceItems = await GetMonitorDevicesAsync();

			return EnumerateMonitors(deviceItems);
		}

		private static HashSet<string> _ids;

		private static async Task<List<DeviceItemPlus>> GetMonitorDevicesAsync()
		{
			IDisplayItem[] displayItems = OsVersion.Is10Redstone4OrNewer
				? await DisplayMonitor.GetDisplayMonitorsAsync()
				: DisplayConfig.EnumerateDisplayConfigs().ToArray();

			var deviceItems = DeviceContext.EnumerateMonitorDevices().ToArray();
			_ids = new HashSet<string>(deviceItems.Select(x => x.DeviceInstanceId));

			IEnumerable<DeviceItemPlus> Enumerate()
			{
				foreach (var deviceItem in deviceItems)
				{
					var displayItem = displayItems.FirstOrDefault(x => string.Equals(deviceItem.DeviceInstanceId, x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
					if (displayItem is not null)
					{
						var isDescriptionNullOrWhiteSpace = string.IsNullOrWhiteSpace(deviceItem.Description);
						if (isDescriptionNullOrWhiteSpace ||
							Regex.IsMatch(deviceItem.Description, "^Generic (?:PnP|Non-PnP) Monitor$", RegexOptions.IgnoreCase))
						{
							if (!string.IsNullOrWhiteSpace(displayItem.DisplayName))
							{
								yield return new DeviceItemPlus(deviceItem, displayItem.DisplayName, displayItem.IsInternal);
								continue;
							}
							if (!isDescriptionNullOrWhiteSpace &&
								!string.IsNullOrWhiteSpace(displayItem.ConnectionDescription))
							{
								yield return new DeviceItemPlus(deviceItem, $"{deviceItem.Description} ({displayItem.ConnectionDescription})", displayItem.IsInternal);
								continue;
							}
						}
					}
					yield return new DeviceItemPlus(deviceItem);
				}
			}

			return Enumerate().Where(x => !string.IsNullOrWhiteSpace(x.AlternateDescription)).ToList();
		}

		private static IEnumerable<IMonitor> EnumerateMonitors(List<DeviceItemPlus> deviceItems)
		{
			if (deviceItems?.Any() is not true)
				yield break;

			var handleItems = DeviceContext.GetMonitorHandles();

			// Obtained by DDC/CI
			foreach (var handleItem in handleItems)
			{
				foreach (var physicalItem in MonitorConfiguration.EnumeratePhysicalMonitors(handleItem.MonitorHandle))
				{
					int index = -1;
					if (physicalItem.IsSupported)
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
						monitorRect: handleItem.MonitorRect,
						handle: physicalItem.Handle,
						useHighLevel: physicalItem.IsHighLevelSupported);

					deviceItems.RemoveAt(index);
					if (deviceItems.Count == 0)
						yield break;
				}
			}

			// Obtained by WMI
			foreach (var desktopItem in MSMonitor.EnumerateDesktopMonitors())
			{
				if (!desktopItem.BrightnessLevels.Any())
					continue;

				foreach (var handleItem in handleItems)
				{
					int index = deviceItems.FindIndex(x =>
						(x.DisplayIndex == handleItem.DisplayIndex) &&
						string.Equals(x.DeviceInstanceId, desktopItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
					if (index < 0)
						continue;

					var deviceItem = deviceItems[index];
					yield return new WmiMonitorItem(
						deviceInstanceId: deviceItem.DeviceInstanceId,
						description: deviceItem.AlternateDescription,
						displayIndex: deviceItem.DisplayIndex,
						monitorIndex: deviceItem.MonitorIndex,
						monitorRect: handleItem.MonitorRect,
						isInternal: deviceItem.IsInternal,
						brightnessLevels: desktopItem.BrightnessLevels);

					deviceItems.RemoveAt(index);
					if (deviceItems.Count == 0)
						yield break;
				}
			}

			// Unreachable neither by DDC/CI nor by WMI
			foreach (var deviceItem in deviceItems)
			{
				yield return new UnreachableMonitorItem(
					deviceInstanceId: deviceItem.DeviceInstanceId,
					description: deviceItem.AlternateDescription,
					displayIndex: deviceItem.DisplayIndex,
					monitorIndex: deviceItem.MonitorIndex,
					isInternal: deviceItem.IsInternal);
			}
		}

		public static bool CheckMonitorsChanged()
		{
			var newIds = new HashSet<string>(DeviceContext.EnumerateMonitorDevices().Select(x => x.DeviceInstanceId));
			var oldIds = _ids;
			_ids = newIds;
			return (oldIds?.SetEquals(newIds) is not true);
		}

		#region Probe

		public static async Task<string> ProbeMonitorsAsync()
		{
			var data = new MonitorData();
			await data.PopulateAsync();

			using (var ms = new MemoryStream())
			using (var jw = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true))
			{
				var serializer = new DataContractJsonSerializer(typeof(MonitorData),
					new DataContractJsonSerializerSettings { SerializeReadOnlyTypes = true });
				serializer.WriteObject(jw, data);
				jw.Flush();
				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}

		[DataContract]
		private class PhysicalItemPlus : MonitorConfiguration.PhysicalItem
		{
			[DataMember(Order = 6)]
			public string GetBrightness { get; private set; }

			[DataMember(Order = 7)]
			public string SetBrightness { get; private set; }

			public PhysicalItemPlus(
				MonitorConfiguration.PhysicalItem item) : base(
					description: item.Description,
					monitorIndex: item.MonitorIndex,
					handle: item.Handle,
					isHighLevelSupported: item.IsHighLevelSupported,
					isLowLevelSupported: item.IsLowLevelSupported,
					capabilitiesString: item.CapabilitiesString,
					capabilitiesReport: item.CapabilitiesReport)
			{
				TestBrightness();
			}

			private void TestBrightness()
			{
				var (getResult, minimum, current, maximum) = MonitorConfiguration.GetBrightness(Handle, IsHighLevelSupported);
				var isGetSuccess = (getResult.Status == AccessStatus.Succeeded);
				var isValid = (minimum < maximum) && (minimum <= current) && (current <= maximum);
				GetBrightness = $"Success: {isGetSuccess}" + (isGetSuccess ? $", Valid: {isValid} (Minimum: {minimum}, Current: {current}, Maximum: {maximum})" : string.Empty);

				var difference = (uint)(DateTime.Now.Ticks % 6 + 5); // Integer from 5 to 10
				var expected = difference;
				if (isGetSuccess && isValid)
				{
					expected = (current - minimum > maximum - current) ? current - difference : current + difference;
					expected = Math.Min(maximum, Math.Max(minimum, expected));
				}

				var setResult = MonitorConfiguration.SetBrightness(Handle, expected, IsHighLevelSupported);
				var isSetSuccess = (setResult.Status == AccessStatus.Succeeded);
				var (_, _, actual, _) = MonitorConfiguration.GetBrightness(Handle, IsHighLevelSupported);
				SetBrightness = $"Success: {isSetSuccess}" + (isSetSuccess ? $", Match: {expected == actual} (Expected: {expected}, Actual: {actual})" : string.Empty);

				if (isSetSuccess)
					MonitorConfiguration.SetBrightness(Handle, current, IsHighLevelSupported);
			}
		}

		[DataContract]
		private class MonitorData
		{
			[DataMember(Order = 0)]
			public string System { get; private set; }

			// When Name property of DataMemberAttribute contains a space or specific character 
			// (e.g. !, ?), DataContractJsonSerializer.WriteObject method will internally throw 
			// a System.Xml.XmlException while it will work fine.
			[DataMember(Order = 1, Name = "Device Context - DeviceItems")]
			public DeviceContext.DeviceItem[] DeviceItems { get; private set; }

			[DataMember(Order = 2, Name = "DisplayMonitor - DisplayItems")]
			public DisplayMonitor.DisplayItem[] DisplayMonitorItems { get; private set; }

			[DataMember(Order = 3, Name = "Display Config - DisplayItems")]
			public DisplayConfig.DisplayItem[] DisplayConfigItems { get; private set; }

			[DataMember(Order = 4, Name = "Device Installation - InstalledItems")]
			public DeviceInformation.InstalledItem[] InstalledItems { get; private set; }

			[DataMember(Order = 5, Name = "Monitor Configuration - PhysicalItems")]
			public Dictionary<DeviceContext.HandleItem, PhysicalItemPlus[]> PhysicalItems { get; private set; }

			[DataMember(Order = 6, Name = "MSMonitorClass - DesktopItems")]
			public MSMonitor.DesktopItem[] DesktopItems { get; private set; }

			[DataMember(Order = 7)]
			public string[] ElapsedTime { get; private set; }

			public MonitorData()
			{ }

			public async Task PopulateAsync()
			{
				System = GetSystem();

				var sw = new Stopwatch();

				var tasks = new[]
				{
					GetTask(nameof(DeviceItems), () =>
						DeviceItems = DeviceContext.EnumerateMonitorDevices().ToArray()),

					GetTask(nameof(DisplayMonitorItems), async () =>
					{
						if (OsVersion.Is10Redstone4OrNewer)
							DisplayMonitorItems = await DisplayMonitor.GetDisplayMonitorsAsync();
					}),

					GetTask(nameof(DisplayConfigItems), () =>
						DisplayConfigItems = DisplayConfig.EnumerateDisplayConfigs().ToArray()),

					GetTask(nameof(InstalledItems), () =>
						InstalledItems = DeviceInformation.EnumerateInstalledMonitors().ToArray()),

					GetTask(nameof(PhysicalItems), () =>
						PhysicalItems = DeviceContext.GetMonitorHandles().ToDictionary(
							x => x,
							x => MonitorConfiguration.EnumeratePhysicalMonitors(x.MonitorHandle, true)
								.Select(x => new PhysicalItemPlus(x))
								.ToArray())),

					GetTask(nameof(DesktopItems), () =>
						DesktopItems = MSMonitor.EnumerateDesktopMonitors().ToArray())
				};

				sw.Start();

				ElapsedTime = await Task.WhenAll(tasks);

				sw.Stop();

				Task<string> GetTask(string name, Action action) =>
					Task.Run(() =>
					{
						action.Invoke();
						var elapsed = sw.Elapsed;
						return $@"{name,-14} -> {elapsed.ToString($@"{(elapsed.Minutes > 0 ? @"m\:" : string.Empty)}s\.fff")}";
					});
			}

			private string GetSystem()
			{
				using var @class = new ManagementClass("Win32_ComputerSystem");
				using var instance = @class.GetInstances().Cast<ManagementObject>().FirstOrDefault();
				return $"Manufacturer: {instance?["Manufacturer"]}, Model: {instance?["Model"]}";
			}
		}

		#endregion
	}
}