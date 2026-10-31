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
using System.Windows;

using Monitorian.Core.Helper;
using Monitorian.Core.Models.Watcher;

namespace Monitorian.Core.Models.Monitor;

internal class MonitorManager
{
	#region Type

	private class DisplayItem
	{
		private readonly DisplayConfig.DisplayItem _deviceConfigItem;

		public string DeviceInstanceId => _deviceConfigItem.DeviceInstanceId;
		public string DisplayName { get; }
		public ConnectionType Connection { get; }
		public bool IsInternal => _deviceConfigItem.IsInternal;
		public DisplayIdSet DisplayIdSet => _deviceConfigItem.DisplayIdSet;

		public DisplayItem(
			DisplayConfig.DisplayItem deviceConfigItem,
			DisplayMonitorProvider.DisplayItem displayMonitorItem)
		{
			this._deviceConfigItem = deviceConfigItem ?? throw new ArgumentNullException(nameof(deviceConfigItem));

			DisplayName = !string.IsNullOrWhiteSpace(displayMonitorItem?.DisplayName)
				? displayMonitorItem.DisplayName
				: deviceConfigItem.DisplayName;

			Connection = (displayMonitorItem is { Connection: not ConnectionType.Unknown })
				? displayMonitorItem.Connection
				: deviceConfigItem.Connection;
		}
	}

	private class BasicItem
	{
		private readonly DeviceContext.DeviceItem _deviceItem;
		private readonly DisplayItem _displayItem;

		public string DeviceInstanceId => _deviceItem.DeviceInstanceId;
		public string Description => _deviceItem.Description;
		public string AlternativeDescription { get; }
		public byte DisplayIndex => _deviceItem.DisplayIndex;
		public byte MonitorIndex => _deviceItem.MonitorIndex;
		public ConnectionType Connection => _displayItem.Connection;
		public bool IsInternal => _displayItem.IsInternal;
		public DisplayIdSet DisplayIdSet => _displayItem.DisplayIdSet;

		public BasicItem(
			DeviceContext.DeviceItem deviceItem,
			DisplayItem displayItem,
			string alternativeDescription)
		{
			this._deviceItem = deviceItem ?? throw new ArgumentNullException(nameof(deviceItem));
			this._displayItem = displayItem ?? throw new ArgumentNullException(nameof(displayItem));

			Debug.Assert(!string.IsNullOrEmpty(alternativeDescription));
			this.AlternativeDescription = alternativeDescription;
		}
	}

	private class DdcMonitorItemFactory : IDisposable
	{
		public BasicItem BasicItem { get; }

		private readonly Rect _monitorRect;
		private readonly SafePhysicalMonitorHandle _handle;
		private MonitorCapability _capability;

		public DdcMonitorItemFactory(
			BasicItem basicItem,
			Rect monitorRect,
			SafePhysicalMonitorHandle handle,
			MonitorCapability capability)
		{
			this.BasicItem = basicItem ?? throw new ArgumentNullException(nameof(basicItem));
			this._monitorRect = monitorRect;
			this._handle = handle ?? throw new ArgumentNullException(nameof(handle));
			this._capability = capability;
		}

		public Task<IDisposable> EnsureCapabilityAsync(Func<SafePhysicalMonitorHandle, MonitorCapability> getCapability)
		{
			if (_capability is null)
			{
				return Task.Run<IDisposable>(() =>
				{
					_capability = getCapability.Invoke(_handle);
					return this;
				});
			}
			return Task.FromResult<IDisposable>(this);
		}

		public DdcMonitorItem GetItem()
		{
			return new DdcMonitorItem(
				deviceInstanceId: BasicItem.DeviceInstanceId,
				description: BasicItem.AlternativeDescription,
				displayIndex: BasicItem.DisplayIndex,
				monitorIndex: BasicItem.MonitorIndex,
				monitorRect: _monitorRect,
				connection: BasicItem.Connection,
				handle: _handle,
				capability: _capability);
		}

		public void Dispose() => _handle.Dispose();
	}

	#endregion

	#region Options

	public static IReadOnlyCollection<string> Options => (new[] { PrecludeOption, PreclearOption })
		.Concat(PowerManagement.Options)
		.Concat(BrightnessConnector.Options)
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
	private static bool _isDisplayMonitorAvailable = true; // Default

	private static async Task<DisplayItem[]> GetDisplayItemsAsync()
	{
		var displayConfigItems = DisplayConfig.EnumerateDisplayConfigs().ToArray();
		DisplayMonitorProvider.DisplayItem[] displayMonitorItems = null;

		if (OsVersion.Is10Build17134OrGreater && _isDisplayMonitorAvailable)
		{
			try
			{
				displayMonitorItems = await DisplayMonitorProvider.GetDisplayMonitorsAsync();
			}
			catch (FileNotFoundException)
			{
				_isDisplayMonitorAvailable = false;
			}
		}

		var displayItems = new DisplayItem[displayConfigItems.Length];
		for (int i = 0; i < displayConfigItems.Length; i++)
		{
			var displayMonitorItem = displayMonitorItems?.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, displayConfigItems[i].DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
			displayItems[i] = new DisplayItem(displayConfigItems[i], displayMonitorItem);
		}
		return displayItems;
	}

	private static IEnumerable<BasicItem> EnumerateBasicItems(DeviceContext.DeviceItem[] deviceItems, DisplayItem[] displayItems)
	{
		foreach (var deviceItem in deviceItems)
		{
			if (_precludedIds.Value.Contains(deviceItem.DeviceInstanceId))
				continue;

			var displayItem = displayItems.FirstOrDefault(x => string.Equals(deviceItem.DeviceInstanceId, x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
			if (displayItem is null)
				continue;

			if (!string.IsNullOrWhiteSpace(displayItem.DisplayName))
			{
				yield return new BasicItem(deviceItem, displayItem, displayItem.DisplayName);
			}
			else if (Regex.IsMatch(deviceItem.Description, "^Generic (?:PnP|Non-PnP) Monitor$", RegexOptions.IgnoreCase)
				&& (displayItem.Connection is not ConnectionType.Unknown))
			{
				yield return new BasicItem(deviceItem, displayItem, $"{deviceItem.Description} ({displayItem.Connection})");
			}
			else if (!string.IsNullOrWhiteSpace(deviceItem.Description))
			{
				yield return new BasicItem(deviceItem, displayItem, deviceItem.Description);
			}
		}
	}

	public static async Task<(IReadOnlyCollection<IMonitor>, IReadOnlyCollection<Task<IDisposable>> runningTasks)> GetMonitorsAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
	{
		var deviceItems = DeviceContext.EnumerateMonitorDevices().ToArray();
		_foundIds = new HashSet<string>(deviceItems.Select(x => x.DeviceInstanceId));

		var displayItems = await GetDisplayItemsAsync();

		var basicItems = EnumerateBasicItems(deviceItems, displayItems).ToList();
		if (basicItems.Count == 0)
			return (Array.Empty<IMonitor>(), Array.Empty<Task<IDisposable>>());

		var handleItems = DeviceContext.GetMonitorHandles();

		var itemList = new List<IMonitor>(basicItems.Count);

		// Controlled under HDR
		if (DisplayInformationWatcher.IsEnabled)
		{
			foreach (var handleItem in handleItems)
			{
				var (isHdr, sdrWhiteLevel) = DisplayInformationProvider.IsHdrAndGetSdrWhiteLevel(handleItem.MonitorHandle);
				if (!isHdr)
					continue;

				int index = basicItems.FindIndex(x =>
					(x.DisplayIndex == handleItem.DisplayIndex));
				if (index < 0)
					continue;

				var basicItem = basicItems[index];
				itemList.Add(new HdrMonitorItem(
					deviceInstanceId: basicItem.DeviceInstanceId,
					description: basicItem.AlternativeDescription,
					displayIndex: basicItem.DisplayIndex,
					monitorIndex: basicItem.MonitorIndex,
					monitorRect: handleItem.MonitorRect,
					connection: basicItem.Connection,
					isInternal: basicItem.IsInternal,
					monitorHandle: handleItem.MonitorHandle,
					displayIdSet: basicItem.DisplayIdSet,
					sdrWhiteLevel: (int)sdrWhiteLevel));

				basicItems.RemoveAt(index);
				if (basicItems.Count == 0)
					return (itemList.ToArray(), Array.Empty<Task<IDisposable>>());
			}
		}

		// Obtained by DDC/CI
		var physicalItemsTasks = handleItems
			.Select(x => Task.Run(() => (
				handleItem: x,
				physicalItems: MonitorConfiguration.EnumeratePhysicalMonitors(x.MonitorHandle).ToArray())))
			.ToArray();

		await Task.WhenAll(physicalItemsTasks);

		var factoryList = new List<DdcMonitorItemFactory>(basicItems.Count);

		foreach ((var handleItem, var physicalItems) in physicalItemsTasks.Select(x => x.Result))
		{
			foreach (var physicalItem in physicalItems)
			{
				var basicItem = basicItems.FirstOrDefault(x =>
					!x.IsInternal &&
					(x.DisplayIndex == handleItem.DisplayIndex) &&
					(x.MonitorIndex == physicalItem.MonitorIndex) &&
					string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
				if (basicItem is null)
				{
					physicalItem.Handle.Dispose();
					continue;
				}

				MonitorCapability capability = null;
				if (_preclearedIds.Value.Contains(basicItem.DeviceInstanceId))
					capability = MonitorCapability.PreclearedCapability;

				factoryList.Add(new DdcMonitorItemFactory(
					basicItem: basicItem,
					monitorRect: handleItem.MonitorRect,
					handle: physicalItem.Handle,
					capability: capability));

				// Continue the loop so that all physical monitor handles are disposed unless taken over.
			}
		}

		var factoryTasks = factoryList
			.Select(x => x.EnsureCapabilityAsync(handle => MonitorConfiguration.GetMonitorCapability(handle, false)))
			.ToList();

		await Task.WhenAny(Task.WhenAll(factoryTasks), Task.Delay(timeout, cancellationToken));
		cancellationToken.ThrowIfCancellationRequested();

		factoryTasks.RemoveAll(x =>
		{
			switch (x.Status)
			{
				case TaskStatus.RanToCompletion:
					var factory = (DdcMonitorItemFactory)x.Result;
					var item = factory.GetItem();
					if (item.IsBrightnessSupported)
					{
						itemList.Add(item);
						basicItems.Remove(factory.BasicItem);
						return true;
					}
					goto default;
				case TaskStatus.Running:
					// Leave as is.
					return false;
				default:
					x.Result.Dispose();
					return true;
			}
		});

		if (basicItems.Count == 0)
			return (itemList.ToArray(), factoryTasks.ToArray());

		// Obtained by WMI
		foreach (var desktopItem in MSMonitor.EnumerateDesktopMonitors())
		{
			if (desktopItem.BrightnessLevels is not { Length: > 0 })
				continue;

			foreach (var handleItem in handleItems)
			{
				int index = basicItems.FindIndex(x =>
					(x.DisplayIndex == handleItem.DisplayIndex) &&
					desktopItem.InstanceName.StartsWith(x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
				if (index < 0)
					continue;

				var basicItem = basicItems[index];
				itemList.Add(new WmiMonitorItem(
					deviceInstanceId: basicItem.DeviceInstanceId,
					description: basicItem.AlternativeDescription,
					displayIndex: basicItem.DisplayIndex,
					monitorIndex: basicItem.MonitorIndex,
					monitorRect: handleItem.MonitorRect,
					connection: basicItem.Connection,
					isInternal: basicItem.IsInternal,
					brightnessLevels: desktopItem.BrightnessLevels));

				basicItems.RemoveAt(index);
				if (basicItems.Count == 0)
					return (itemList.ToArray(), factoryTasks.ToArray());
			}
		}

		// Unreachable by HDR, DDC/CI (including factory task is still running), or WMI
		foreach (var basicItem in basicItems)
		{
			itemList.Add(new UnreachableMonitorItem(
				deviceInstanceId: basicItem.DeviceInstanceId,
				description: basicItem.AlternativeDescription,
				displayIndex: basicItem.DisplayIndex,
				monitorIndex: basicItem.MonitorIndex,
				connection: basicItem.Connection,
				isInternal: basicItem.IsInternal));
		}

		return (itemList.ToArray(), factoryTasks.ToArray());
	}

	public static bool CheckMonitorsChanged()
	{
		if ((_foundIds is not null) &&
			(_foundIds.Count != SystemMetric.GetMonitorCount()))
		{
			return true;
		}

		var oldIds = _foundIds;
		_foundIds = new HashSet<string>(DeviceContext.EnumerateMonitorDevices().Select(x => x.DeviceInstanceId));
		return (oldIds?.SetEquals(_foundIds) is false);
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
	private class DisplayItemPlus
	{
		[DataMember]
		public int DisplayIndex { get; private set; }

		[DataMember]
		public DisplayInformationProvider.DisplayItem DisplayItem { get; }

		public DisplayItemPlus(DeviceContext.HandleItem handleItem)
		{
			DisplayIndex = handleItem.DisplayIndex;
			DisplayItem = new DisplayInformationProvider.DisplayItem(handleItem.MonitorHandle);
		}
	}

	[DataContract]
	private class InstalledItemPlus : DeviceInformation.InstalledItem
	{
		[DataMember(Order = 3)]
		public string Manufacture
		{ get; private set; }

		[DataMember(Order = 4)]
		public string Date { get; private set; }

		public InstalledItemPlus(DeviceInformation.InstalledItem installedItem) : base(
			deviceInstanceId: installedItem.DeviceInstanceId,
			description: installedItem.Description,
			isRemovable: installedItem.IsRemovable)
		{
			var edid = EdidInfo.ReadFromRegistry(installedItem.DeviceInstanceId);
			if (edid is not null)
			{
				Manufacture = edid.Manufacturer;
				Date = $"{edid.ManufactureYear}-W{edid.ManufactureWeek:D2}";
			}
		}
	}

	[DataContract]
	private class PhysicalItemPlus : MonitorConfiguration.PhysicalItem
	{
		[DataMember(Order = 2)]
		public MonitorCapability Capability { get; }

		[DataMember(Order = 3)]
		public string GetBrightness { get; private set; }

		[DataMember(Order = 4)]
		public string SetBrightness { get; private set; }

		[DataMember(Order = 5)]
		public string GetContrast { get; private set; }

		[DataMember(Order = 6)]
		public string SetContrast { get; private set; }

		public PhysicalItemPlus(
			MonitorConfiguration.PhysicalItem item,
			MonitorCapability capability) : base(
				description: item.Description,
				monitorIndex: item.MonitorIndex,
				handle: item.Handle)
		{
			this.Capability = capability;

			TestBrightness();
			TestContrast();
		}

		private void TestBrightness()
		{
			var (getResult, minimum, current, maximum) = MonitorConfiguration.GetBrightness(Handle, Capability.IsHighLevelBrightnessSupported);
			var getSuccess = (getResult.Status is AccessStatus.Succeeded);
			var (isValid, expected) = GetExpected(getSuccess, minimum, current, maximum);
			GetBrightness = $"Success: {getSuccess}" + (getSuccess ? $", Valid: {isValid} (Minimum: {minimum}, Current: {current}, Maximum: {maximum})" : string.Empty);

			var setResult = MonitorConfiguration.SetBrightness(Handle, expected, Capability.IsHighLevelBrightnessSupported);
			var setSuccess = (setResult.Status is AccessStatus.Succeeded);
			var (_, _, actual, _) = MonitorConfiguration.GetBrightness(Handle, Capability.IsHighLevelBrightnessSupported);
			SetBrightness = $"Success: {setSuccess}" + (setSuccess ? $", Match: {expected == actual} (Expected: {expected}, Actual: {actual})" : string.Empty);

			if (setSuccess)
				MonitorConfiguration.SetBrightness(Handle, current, Capability.IsHighLevelBrightnessSupported);
		}

		private void TestContrast()
		{
			var (getResult, minimum, current, maximum) = MonitorConfiguration.GetContrast(Handle);
			var getSuccess = (getResult.Status is AccessStatus.Succeeded);
			var (isValid, expected) = GetExpected(getSuccess, minimum, current, maximum);
			GetContrast = $"Success: {getSuccess}" + (getSuccess ? $", Valid: {isValid} (Minimum: {minimum}, Current: {current}, Maximum: {maximum})" : string.Empty);

			var setResult = MonitorConfiguration.SetContrast(Handle, expected);
			var setSuccess = (setResult.Status is AccessStatus.Succeeded);
			var (_, _, actual, _) = MonitorConfiguration.GetContrast(Handle);
			SetContrast = $"Success: {setSuccess}" + (setSuccess ? $", Match: {expected == actual} (Expected: {expected}, Actual: {actual})" : string.Empty);

			if (setSuccess)
				MonitorConfiguration.SetContrast(Handle, current);
		}

		private static (bool, uint) GetExpected(bool success, uint minimum, uint current, uint maximum)
		{
			const uint m = 5;

			var isValid = (maximum - minimum >= m * 2) && (minimum <= current) && (current <= maximum);

			if (!success)
			{
				return (isValid, (uint)(DateTime.Now.Ticks % 101));
			}

			uint lower = current - (isValid ? minimum : 0);
			uint higher = (isValid ? maximum : 100) - current;

			if (lower > higher)
			{
				return (isValid, current - m - (uint)(DateTime.Now.Ticks % (lower - m + 1)));
			}
			else
			{
				return (isValid, current + m + (uint)(DateTime.Now.Ticks % (higher - m + 1)));
			}
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

		[DataMember(Order = 2, Name = "Display Monitor - DisplayItems")]
		public DisplayMonitorProvider.DisplayItem[] DisplayMonitorItems { get; private set; }

		[DataMember(Order = 3, Name = "Display Config - DisplayItems")]
		public DisplayConfig.DisplayItem[] DisplayConfigItems { get; private set; }

		[DataMember(Order = 4, Name = "Display Information - DisplayItems")]
		public DisplayItemPlus[] DisplayInformationItems { get; private set; }

		[DataMember(Order = 5, Name = "Device Installation - InstalledItems")]
		public InstalledItemPlus[] InstalledItems { get; private set; }

		[DataMember(Order = 6, Name = "Monitor Configuration - PhysicalItems")]
		public Dictionary<DeviceContext.HandleItem, PhysicalItemPlus[]> PhysicalItems { get; private set; }

		[DataMember(Order = 7, Name = "MSMonitorClass - DesktopItems")]
		public MSMonitor.DesktopItem[] DesktopItems { get; private set; }

		[DataMember(Order = 8)]
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
					try
					{
						DisplayMonitorItems = await DisplayMonitorProvider.GetDisplayMonitorsAsync();
					}
					catch (Exception)
					{
					}
				}),

				GetTask(nameof(DisplayConfigItems), () =>
					DisplayConfigItems = DisplayConfig.EnumerateDisplayConfigs().ToArray()),

				GetTask(nameof(DisplayInformationItems), () =>
					DisplayInformationItems = DeviceContext.GetMonitorHandles()
						.Select(x => new DisplayItemPlus(x)).ToArray()),

				GetTask(nameof(InstalledItems), () =>
					InstalledItems = DeviceInformation.EnumerateInstalledMonitors()
						.Select(x => new InstalledItemPlus(x)).ToArray()),

				GetTask(nameof(PhysicalItems), () =>
					PhysicalItems = DeviceContext.GetMonitorHandles().ToDictionary(
						x => x,
						x => MonitorConfiguration.EnumeratePhysicalMonitors(x.MonitorHandle)
							.Select(x => new PhysicalItemPlus(x, MonitorConfiguration.GetMonitorCapability(x.Handle, true)))
							.ToArray())),

				GetTask(nameof(DesktopItems), () =>
					DesktopItems = MSMonitor.EnumerateDesktopMonitors(true).ToArray())
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