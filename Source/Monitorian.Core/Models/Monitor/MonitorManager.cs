using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Monitorian.Core.Helper;
using Monitorian.Core.Models.Watcher;

namespace Monitorian.Core.Models.Monitor;

internal class MonitorManager
{
	#region Type

	private class BasicItem
	{
		private readonly DeviceContext.DeviceItem _deviceItem;

		public string DeviceInstanceId => _deviceItem.DeviceInstanceId;
		public string Description => _deviceItem.Description;
		public string AlternateDescription { get; }
		public byte DisplayIndex => _deviceItem.DisplayIndex;
		public byte MonitorIndex => _deviceItem.MonitorIndex;
		public bool IsInternal { get; }

		public BasicItem(
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

	#region Options

	public static IReadOnlyCollection<string> Options => (new[] { PrecludeOption, PreclearOption })
		.Concat(PowerManagement.Options)
		.Concat(BrightnessConnector.Options)
		.Concat(DisplayInformationWatcher.Options)
		.ToArray();

	private const string PrecludeOption = "/preclude";
	private const string PreclearOption = "/preclear";

	private static readonly Lazy<HashSet<string>> _precludedIds = new(() => GetOptionIds(PrecludeOption));
	private static readonly Lazy<HashSet<string>> _preclearedIds = new(() => GetOptionIds(PreclearOption));

	private static HashSet<string> GetOptionIds(string option)
	{
		var ids = AppKeeper.StandardArguments
			.SkipWhile(x => !string.Equals(x, option, StringComparison.OrdinalIgnoreCase))
			.Skip(1) // 1 means option.
			.Select(x => (success: DeviceConversion.TryParseToDeviceInstanceId(x, out string id), id))
			.TakeWhile(x => x.success)
			.Select(x => x.id);

		return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
	}

	#endregion

	private static HashSet<string> _foundIds;

	public static async Task<IEnumerable<IMonitor>> EnumerateMonitorsAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
	{
		var deviceItems = DeviceContext.EnumerateMonitorDevices().ToArray();
		_foundIds = new HashSet<string>(deviceItems.Select(x => x.DeviceInstanceId));

		IDisplayItem[] displayItems = OsVersion.Is10Build17134OrGreater
			? await DisplayMonitorProvider.GetDisplayMonitorsAsync()
			: DisplayConfig.EnumerateDisplayConfigs().ToArray();

		IEnumerable<BasicItem> EnumerateBasicItems()
		{
			foreach (var deviceItem in deviceItems)
			{
				if (_precludedIds.Value.Any(x => string.Equals(deviceItem.DeviceInstanceId, x, StringComparison.OrdinalIgnoreCase)))
					continue;

				var displayItem = displayItems.FirstOrDefault(x => string.Equals(deviceItem.DeviceInstanceId, x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
				if (displayItem is null)
				{
					yield return new BasicItem(deviceItem);
				}
				else if (!string.IsNullOrWhiteSpace(displayItem.DisplayName))
				{
					yield return new BasicItem(deviceItem, displayItem.DisplayName, displayItem.IsInternal);
				}
				else if (Regex.IsMatch(deviceItem.Description, "^Generic (?:PnP|Non-PnP) Monitor$", RegexOptions.IgnoreCase)
					&& !string.IsNullOrWhiteSpace(displayItem.ConnectionDescription))
				{
					yield return new BasicItem(deviceItem, $"{deviceItem.Description} ({displayItem.ConnectionDescription})", displayItem.IsInternal);
				}
				else
				{
					yield return new BasicItem(deviceItem, null, displayItem.IsInternal);
				}
			}
		}

		var basicItems = EnumerateBasicItems().Where(x => !string.IsNullOrWhiteSpace(x.AlternateDescription)).ToList();
		if (basicItems.Count == 0)
			return Enumerable.Empty<IMonitor>();

		var handleItems = DeviceContext.GetMonitorHandles();

		var physicalItemsTasks = handleItems
			.Select(x => Task.Run(() => (x, physicalItems: MonitorConfiguration.EnumeratePhysicalMonitors(x.MonitorHandle))))
			.ToArray();

		await Task.WhenAny(Task.WhenAll(physicalItemsTasks), Task.Delay(timeout, cancellationToken));
		cancellationToken.ThrowIfCancellationRequested();

		var physicalItemsPairs = physicalItemsTasks.Where(x => x.Status == TaskStatus.RanToCompletion).Select(x => x.Result);

		IEnumerable<IMonitor> EnumerateMonitorItems()
		{
			// Obtained by DDC/CI
			foreach ((var handleItem, var physicalItems) in physicalItemsPairs)
			{
				foreach (var physicalItem in physicalItems)
				{
					int index = -1;
					if (physicalItem.Capability.IsBrightnessSupported ||
						_preclearedIds.Value.Any())
					{
						index = basicItems.FindIndex(x =>
							!x.IsInternal &&
							(x.DisplayIndex == handleItem.DisplayIndex) &&
							(x.MonitorIndex == physicalItem.MonitorIndex) &&
							string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
					}
					if (index < 0)
					{
						physicalItem.Handle.Dispose();
						continue;
					}

					var basicItem = basicItems[index];

					MonitorCapability capability = null;
					if (physicalItem.Capability.IsBrightnessSupported)
					{
						capability = physicalItem.Capability;
					}
					else if (_preclearedIds.Value.Contains(basicItem.DeviceInstanceId))
					{
						capability = MonitorCapability.PreclearedCapability;
					}
					else
					{
						physicalItem.Handle.Dispose();
						continue;
					}

					yield return new DdcMonitorItem(
						deviceInstanceId: basicItem.DeviceInstanceId,
						description: basicItem.AlternateDescription,
						displayIndex: basicItem.DisplayIndex,
						monitorIndex: basicItem.MonitorIndex,
						monitorRect: handleItem.MonitorRect,
						handle: physicalItem.Handle,
						capability: capability,
						onDisposed: DisplayInformationWatcher.RegisterMonitor(basicItem.DeviceInstanceId, handleItem.MonitorHandle));

					basicItems.RemoveAt(index);
					if (basicItems.Count == 0)
						yield break;
				}
			}

			// Obtained by WMI
			foreach (var desktopItem in MSMonitor.EnumerateDesktopMonitors())
			{
				if (desktopItem.BrightnessLevels is not { Length: > 0 })
					continue;

				foreach (var handleItem in handleItems)
				{
					int index = basicItems.FindIndex(x =>
						(x.DisplayIndex == handleItem.DisplayIndex) &&
						string.Equals(x.DeviceInstanceId, desktopItem.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
					if (index < 0)
						continue;

					var basicItem = basicItems[index];
					yield return new WmiMonitorItem(
						deviceInstanceId: basicItem.DeviceInstanceId,
						description: basicItem.AlternateDescription,
						displayIndex: basicItem.DisplayIndex,
						monitorIndex: basicItem.MonitorIndex,
						monitorRect: handleItem.MonitorRect,
						isInternal: basicItem.IsInternal,
						brightnessLevels: desktopItem.BrightnessLevels,
						onDisposed: DisplayInformationWatcher.RegisterMonitor(basicItem.DeviceInstanceId, handleItem.MonitorHandle));

					basicItems.RemoveAt(index);
					if (basicItems.Count == 0)
						yield break;
				}
			}

			// Unreachable neither by DDC/CI nor by WMI
			foreach (var basicItem in basicItems)
			{
				yield return new UnreachableMonitorItem(
					deviceInstanceId: basicItem.DeviceInstanceId,
					description: basicItem.AlternateDescription,
					displayIndex: basicItem.DisplayIndex,
					monitorIndex: basicItem.MonitorIndex,
					isInternal: basicItem.IsInternal);
			}
		}

		return EnumerateMonitorItems();
	}

	public static bool CheckMonitorsChanged()
	{
		var oldIds = _foundIds;
		_foundIds = new HashSet<string>(DeviceContext.EnumerateMonitorDevices().Select(x => x.DeviceInstanceId));
		return (oldIds?.SetEquals(_foundIds) is not true);
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
		[DataMember(Order = 3)]
		public string GetBrightness { get; private set; }

		[DataMember(Order = 4)]
		public string SetBrightness { get; private set; }

		public PhysicalItemPlus(
			MonitorConfiguration.PhysicalItem item) : base(
				description: item.Description,
				monitorIndex: item.MonitorIndex,
				handle: item.Handle,
				capability: item.Capability)
		{
			TestBrightness();
		}

		private void TestBrightness()
		{
			var (getResult, minimum, current, maximum) = MonitorConfiguration.GetBrightness(Handle, Capability.IsHighLevelBrightnessSupported);
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

			var setResult = MonitorConfiguration.SetBrightness(Handle, expected, Capability.IsHighLevelBrightnessSupported);
			var isSetSuccess = (setResult.Status == AccessStatus.Succeeded);
			var (_, _, actual, _) = MonitorConfiguration.GetBrightness(Handle, Capability.IsHighLevelBrightnessSupported);
			SetBrightness = $"Success: {isSetSuccess}" + (isSetSuccess ? $", Match: {expected == actual} (Expected: {expected}, Actual: {actual})" : string.Empty);

			if (isSetSuccess)
				MonitorConfiguration.SetBrightness(Handle, current, Capability.IsHighLevelBrightnessSupported);
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
		public DisplayMonitorProvider.DisplayItem[] DisplayMonitorItems { get; private set; }

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
			System = $"Manufacturer: {SystemInfo.Manufacturer}, Model: {SystemInfo.Model}, OS: {Environment.OSVersion.Version}";

			var sw = new Stopwatch();

			var tasks = new[]
			{
				GetTask(nameof(DeviceItems), () =>
					DeviceItems = DeviceContext.EnumerateMonitorDevices().ToArray()),

				GetTask(nameof(DisplayMonitorItems), async () =>
				{
					if (OsVersion.Is10Build17134OrGreater)
						DisplayMonitorItems = await DisplayMonitorProvider.GetDisplayMonitorsAsync();
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
	}

	#endregion
}