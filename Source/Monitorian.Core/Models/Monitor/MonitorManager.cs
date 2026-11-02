using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
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

	public class MonitorItemSource : IMonitorSource
	{
		private readonly IMonitor _monitor;

		public string DeviceInstanceId => _monitor.DeviceInstanceId;

		public MonitorItemSource(IMonitor monitor) => this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

		public Task<IMonitor> GetMonitorAsync(TimeSpan timeout) => Task.FromResult(_monitor);

		public override string ToString()
		{
			return SimpleSerialization.Serialize(
				(nameof(Type), $"{_monitor.GetType().Name}Source"),
				(nameof(DeviceInstanceId), DeviceInstanceId),
				(nameof(IMonitor.Description), _monitor.Description),
				(nameof(IMonitor.DisplayIndex), _monitor.DisplayIndex),
				(nameof(IMonitor.MonitorIndex), _monitor.MonitorIndex),
				(nameof(IMonitor.MonitorRect), _monitor.MonitorRect),
				(nameof(IMonitor.Connection), _monitor.Connection));
		}

		public void Dispose() { }
	}

	public class DdcMonitorItemSource : IMonitorSource
	{
		public string DeviceInstanceId { get; }
		private readonly string _description;
		private readonly byte _displayIndex;
		private readonly byte _monitorIndex;
		private readonly Rect _monitorRect;
		private readonly ConnectionType _connection;
		private SafePhysicalMonitorHandle _handle;
		private MonitorCapability _capability;

		public DdcMonitorItemSource(
			string deviceInstanceId,
			string description,
			byte displayIndex,
			byte monitorIndex,
			Rect monitorRect,
			ConnectionType connection,
			SafePhysicalMonitorHandle handle,
			MonitorCapability capability)
		{
			this.DeviceInstanceId = deviceInstanceId;
			this._description = description;
			this._displayIndex = displayIndex;
			this._monitorIndex = monitorIndex;
			this._monitorRect = monitorRect;
			this._connection = connection;
			this._handle = handle ?? throw new ArgumentNullException(nameof(handle));
			this._capability = capability;
		}

		private bool? _success = null;

		public async Task<IMonitor> GetMonitorAsync(TimeSpan timeout)
		{
			if (_success is not null)
				throw new InvalidOperationException($"{nameof(GetMonitorAsync)} can only be called once.");

			if (_capability is null)
			{
				var getTask = Task.Factory.StartNew(() => MonitorConfiguration.GetMonitorCapability(_handle, false),
					default, TaskCreationOptions.LongRunning, TaskScheduler.Default);

				if (getTask == await Task.WhenAny(getTask, Task.Delay(timeout)))
					_capability = getTask.Result;
			}
			if (_capability is { IsBrightnessSupported: true })
			{
				try
				{
					_success = true;
					return new DdcMonitorItem(
						deviceInstanceId: DeviceInstanceId,
						description: _description,
						displayIndex: _displayIndex,
						monitorIndex: _monitorIndex,
						monitorRect: _monitorRect,
						connection: _connection,
						handle: _handle,
						capability: _capability);
				}
				finally
				{
					_handle = null; // Ownership transferred
				}
			}
			_success = false;
			return new UnreachableMonitorItem(
				deviceInstanceId: DeviceInstanceId,
				description: _description,
				displayIndex: _displayIndex,
				monitorIndex: _monitorIndex,
				connection: _connection,
				isInternal: false);
		}

		public override string ToString()
		{
			return SimpleSerialization.Serialize(
				(nameof(Type), this.GetType().Name),
				(nameof(DeviceInstanceId), DeviceInstanceId),
				(nameof(IMonitor.Description), _description),
				(nameof(IMonitor.DisplayIndex), _displayIndex),
				(nameof(IMonitor.MonitorIndex), _monitorIndex),
				(nameof(IMonitor.MonitorRect), _monitorRect),
				(nameof(IMonitor.Connection), _connection),
				("Success", _success),
				(nameof(MonitorCapability.Elapsed), _capability?.Elapsed));
		}

		public void Dispose() => _handle?.Dispose();
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

	public static async Task<IReadOnlyCollection<IMonitorSource>> GetMonitorsAsync()
	{
		var deviceItems = DeviceContext.EnumerateMonitorDevices().ToArray();
		_foundIds = new HashSet<string>(deviceItems.Select(x => x.DeviceInstanceId));

		var displayItems = await GetDisplayItemsAsync();

		var basicItems = EnumerateBasicItems(deviceItems, displayItems).ToList();
		if (basicItems.Count == 0)
			return Array.Empty<IMonitorSource>();

		var handleItems = DeviceContext.GetMonitorHandles();

		var sources = new List<IMonitorSource>(basicItems.Count);

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
				sources.Add(new MonitorItemSource(new HdrMonitorItem(
					deviceInstanceId: basicItem.DeviceInstanceId,
					description: basicItem.AlternativeDescription,
					displayIndex: basicItem.DisplayIndex,
					monitorIndex: basicItem.MonitorIndex,
					monitorRect: handleItem.MonitorRect,
					connection: basicItem.Connection,
					isInternal: basicItem.IsInternal,
					monitorHandle: handleItem.MonitorHandle,
					displayIdSet: basicItem.DisplayIdSet,
					sdrWhiteLevel: (int)sdrWhiteLevel)));

				basicItems.RemoveAt(index);
				if (basicItems.Count == 0)
					return sources.ToArray();
			}
		}

		// Obtained by DDC/CI
		var physicalItemsTasks = handleItems
			.Select(x => Task.Run(() => (
				handleItem: x,
				physicalItems: MonitorConfiguration.EnumeratePhysicalMonitors(x.MonitorHandle).ToArray())))
			.ToArray();

		await Task.WhenAll(physicalItemsTasks);

		foreach ((var handleItem, var physicalItems) in physicalItemsTasks.Select(x => x.Result))
		{
			foreach (var physicalItem in physicalItems)
			{
				int index = basicItems.FindIndex(x =>
					!x.IsInternal &&
					(x.DisplayIndex == handleItem.DisplayIndex) &&
					(x.MonitorIndex == physicalItem.MonitorIndex) &&
					string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
				if (index < 0)
				{
					physicalItem.Handle.Dispose();
					continue;
				}

				var basicItem = basicItems[index];

				MonitorCapability capability = null;
				if (_preclearedIds.Value.Contains(basicItem.DeviceInstanceId))
					capability = MonitorCapability.PreclearedCapability;

				sources.Add(new DdcMonitorItemSource(
					deviceInstanceId: basicItem.DeviceInstanceId,
					description: basicItem.AlternativeDescription,
					displayIndex: basicItem.DisplayIndex,
					monitorIndex: basicItem.MonitorIndex,
					monitorRect: handleItem.MonitorRect,
					connection: basicItem.Connection,
					handle: physicalItem.Handle,
					capability: capability));

				basicItems.RemoveAt(index);
				// Continue the loop so that all physical monitor handles are disposed unless taken over.
			}
		}

		if (basicItems.Count == 0)
			return sources.ToArray();

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
				sources.Add(new MonitorItemSource(new WmiMonitorItem(
					deviceInstanceId: basicItem.DeviceInstanceId,
					description: basicItem.AlternativeDescription,
					displayIndex: basicItem.DisplayIndex,
					monitorIndex: basicItem.MonitorIndex,
					monitorRect: handleItem.MonitorRect,
					connection: basicItem.Connection,
					isInternal: basicItem.IsInternal,
					brightnessLevels: desktopItem.BrightnessLevels)));

				basicItems.RemoveAt(index);
				if (basicItems.Count == 0)
					return sources.ToArray();
			}
		}

		// Unreachable by HDR, DDC/CI, or WMI
		foreach (var basicItem in basicItems)
		{
			sources.Add(new MonitorItemSource(new UnreachableMonitorItem(
				deviceInstanceId: basicItem.DeviceInstanceId,
				description: basicItem.AlternativeDescription,
				displayIndex: basicItem.DisplayIndex,
				monitorIndex: basicItem.MonitorIndex,
				connection: basicItem.Connection,
				isInternal: basicItem.IsInternal)));
		}

		return sources.ToArray();
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
		public string Manufacture { get; private set; }

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

			var startingTimeStamp = Stopwatch.GetTimestamp();

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

			ElapsedTime = await Task.WhenAll(tasks);

			Task<string> GetTask(string name, Action action) =>
				Task.Run(() =>
				{
					action.Invoke();
					var elapsed = StopWatchAddition.GetElapsedTotalSeconds(startingTimeStamp);
					return $@"{name,-23} -> {elapsed:F3}";
				});
		}
	}

	#endregion
}