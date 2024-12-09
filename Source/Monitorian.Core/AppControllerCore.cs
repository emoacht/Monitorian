using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;
using Monitorian.Core.Models.Watcher;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views;
using ScreenFrame;
using StartupAgency;

namespace Monitorian.Core;

public class AppControllerCore
{
	protected readonly Application _current = Application.Current;

	protected readonly AppKeeper _keeper;
	protected internal StartupAgent StartupAgent => _keeper.StartupAgent;

	protected internal SettingsCore Settings { get; }

	public ObservableCollection<MonitorViewModel> Monitors { get; }
	protected readonly object _monitorsLock = new();

	public NotifyIconContainer NotifyIconContainer { get; }
	public WindowPainter WindowPainter { get; }

	private readonly SessionWatcher _sessionWatcher;
	private readonly PowerWatcher _powerWatcher;
	private readonly DisplaySettingsWatcher _displaySettingsWatcher;
	private readonly DisplayInformationWatcher _displayInformationWatcher;
	private readonly BrightnessWatcher _brightnessWatcher;
	private readonly BrightnessConnector _brightnessConnector;

	public AppControllerCore(AppKeeper keeper, SettingsCore settings)
	{
		this._keeper = keeper ?? throw new ArgumentNullException(nameof(keeper));
		this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));

		LanguageService.SwitchDefault();

		Monitors = new ObservableCollection<MonitorViewModel>();
		BindingOperations.EnableCollectionSynchronization(Monitors, _monitorsLock);

		NotifyIconContainer = new NotifyIconContainer();
		WindowPainter = new WindowPainter();

		_sessionWatcher = new SessionWatcher();
		_powerWatcher = new PowerWatcher();
		_displaySettingsWatcher = new DisplaySettingsWatcher();
		_displayInformationWatcher = new DisplayInformationWatcher();
		_brightnessWatcher = new BrightnessWatcher();
		_brightnessConnector = new BrightnessConnector();
	}

	public virtual async Task InitiateAsync()
	{
		await Settings.InitiateAsync();
		Settings.MonitorCustomizations.AbsoluteCapacity = MaxKnownMonitorsCount;
		Settings.PropertyChanged += OnSettingsChanged;

		OnSettingsInitiated();

		NotifyIconContainer.ShowIcon(WindowPainter.GetIconPath(), ProductInfo.Title);
		WindowPainter.ThemeChanged += (_, _) =>
		{
			NotifyIconContainer.ShowIcon(WindowPainter.GetIconPath());
		};

		_current.MainWindow = new MainWindow(this);

		if (StartupAgent.IsWindowShowExpected())
			_current.MainWindow.Show();

		await ScanAsync();

		StartupAgent.HandleRequestAsync = HandleRequestAsync;

		NotifyIconContainer.MouseLeftButtonClick += OnMainWindowShowRequestedBySelf;
		NotifyIconContainer.MouseRightButtonClick += OnMenuWindowShowRequested;

		if (ViewManager.IsIconWheelEnabled())
			NotifyIconContainer.MouseWheel += (_, delta) => ReflectMouseWheel(delta);

		_sessionWatcher.Subscribe((e) => OnMonitorsChangeInferred(nameof(SessionWatcher), e));
		_powerWatcher.Subscribe((e) => OnMonitorsChangeInferred(nameof(PowerWatcher), e));
		_displaySettingsWatcher.Subscribe((e) =>
		{
			if (!_powerWatcher.IsDisplayOff)
			{
				OnMonitorsChangeInferred(nameof(DisplaySettingsWatcher), e);
			}
		});

		_displayInformationWatcher.Subscribe(async (deviceInstanceId, message) =>
		{
			if (!_sessionWatcher.IsLocked)
			{
				await UpdateMessageAsync(deviceInstanceId, message);
				await OperationRecorder.RecordAsync(message);
			}
		});

		if (Monitors.Any(x => x.IsInternal))
		{
			_brightnessWatcher.Subscribe((instanceName, brightness) =>
			{
				if (!_sessionWatcher.IsLocked)
					Update(instanceName, brightness);
			},
			async (message) => await OperationRecorder.RecordAsync(message));

			if (_brightnessConnector.IsEnabled)
			{
				await _brightnessConnector.InitiateAsync((brightness) =>
				{
					if (!_sessionWatcher.IsLocked)
						Update(null, brightness);
				},
				async (message) => await OperationRecorder.RecordAsync(message),
				() => _current.Dispatcher.Invoke(() => _current.MainWindow.Visibility is Visibility.Visible));
			}
		}

		await CleanAsync();
	}

	public virtual void End()
	{
		MonitorsDispose();

		NotifyIconContainer.Dispose();
		WindowPainter.Dispose();

		_sessionWatcher.Dispose();
		_powerWatcher.Dispose();
		_displaySettingsWatcher.Dispose();
		_displayInformationWatcher.Dispose();
		_brightnessWatcher.Dispose();
		_brightnessConnector.Dispose();
	}

	protected virtual Task<string> HandleRequestAsync(IReadOnlyCollection<string> args)
	{
		OnMainWindowShowRequestedByOther(null, EventArgs.Empty);
		return Task.FromResult<string>(null);
	}

	protected async void OnMainWindowShowRequestedBySelf(object sender, EventArgs e)
	{
		ShowMainWindow();
		await CheckUpdateAsync();

		if (_brightnessConnector.IsEnabled)
			await _brightnessConnector.ConnectAsync(true);
	}

	protected async void OnMainWindowShowRequestedByOther(object sender, EventArgs e)
	{
		_current.Dispatcher.Invoke(() => ShowMainWindow());
		await CheckUpdateAsync();

		if (_brightnessConnector.IsEnabled)
			await _brightnessConnector.ConnectAsync(true);
	}

	protected void OnMenuWindowShowRequested(object sender, Point e)
	{
		ShowMenuWindow(e);
	}

	protected virtual void ShowMainWindow()
	{
		var window = (MainWindow)_current.MainWindow;
		if (window is { CanBeShown: false } or { Visibility: Visibility.Visible, IsForeground: true })
			return;

		window.ShowForeground();
		window.Activate();
	}

	protected virtual void HideMainWindow()
	{
		var window = (MainWindow)_current.MainWindow;
		if (window is { Visibility: Visibility.Hidden })
			return;

		window.ClearHide();
	}

	protected virtual void ShowMenuWindow(Point pivot)
	{
		var window = new MenuWindow(this, pivot);
		window.ViewModel.CloseAppRequested += (_, _) => _current.Shutdown();
		window.MenuSectionTop.Add(new DevSection(this));
		window.Show();
	}

	protected virtual async void OnSettingsInitiated()
	{
		if (Settings.UsesAccentColor)
			WindowPainter.AttachAccentColors();

		if (Settings.MakesOperationLog)
			await OperationRecorder.EnableAsync("Initiated");
	}

	protected virtual async void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(Settings.UsesAccentColor):
				if (Settings.UsesAccentColor)
					WindowPainter.AttachAccentColors();
				else
					WindowPainter.DetachAccentColors();

				break;

			case nameof(Settings.EnablesUnison) when !Settings.EnablesUnison:
				foreach (var m in Monitors)
					m.IsUnison = false;

				break;

			case nameof(Settings.EnablesRange) when !Settings.EnablesRange:
				foreach (var m in Monitors)
					m.IsRangeChanging = false;

				break;

			case nameof(Settings.EnablesContrast) when !Settings.EnablesContrast:
				foreach (var m in Monitors)
					m.IsContrastChanging = false;

				break;

			case nameof(Settings.MakesOperationLog):
				if (Settings.MakesOperationLog)
					await OperationRecorder.EnableAsync("Enabled");
				else
					OperationRecorder.Disable();

				break;
		}
	}

	#region Monitors

	protected virtual async void OnMonitorsChangeInferred(object sender, ICountEventArgs e = null)
	{
		await OperationRecorder.RecordAsync($"{nameof(OnMonitorsChangeInferred)} ({sender}{e?.Description})");

		await ProceedScanAsync(e);
	}

	protected virtual async Task ProceedScanAsync(ICountEventArgs e)
	{
		if (e is { Count: 0 })
			return;

		await ScanAsync(TimeSpan.FromSeconds(3));
	}

	protected internal virtual async void OnMonitorAccessFailed(AccessResult result)
	{
		await OperationRecorder.RecordAsync($"{nameof(OnMonitorAccessFailed)}" + Environment.NewLine
			+ $"Status: {result.Status}" + Environment.NewLine
			+ $"Message: {result.Message}");
	}

	protected internal virtual async void OnMonitorsChangeFound()
	{
		if (Monitors.Any())
		{
			await OperationRecorder.RecordAsync($"{nameof(OnMonitorsChangeFound)}");

			_displaySettingsWatcher.RaiseDisplaySettingsChanged();
		}
	}

	internal event EventHandler<bool> ScanningChanged;

	protected virtual Task<byte> GetMaxMonitorsCountAsync() => Task.FromResult<byte>(4);
	protected const int MaxKnownMonitorsCount = 64;

	protected virtual MonitorViewModel GetMonitor(IMonitor monitorItem) => new MonitorViewModel(this, monitorItem);
	protected virtual void DisposeMonitor(MonitorViewModel monitor) => monitor?.Dispose();

	private int _scanCount = 0;
	private int _updateCount = 0;

	internal Task ScanAsync() => ScanAsync(TimeSpan.Zero);

	protected virtual async Task ScanAsync(TimeSpan interval)
	{
		var isEntered = false;
		try
		{
			isEntered = (Interlocked.Increment(ref _scanCount) == 1);
			if (isEntered)
			{
				ScanningChanged?.Invoke(this, true);

				var intervalTask = (interval > TimeSpan.Zero) ? Task.Delay(interval) : Task.CompletedTask;

				OperationRecorder.StartGroupRecord($"{nameof(ScanAsync)} [{DateTime.Now}]");

				await Task.Run(async () =>
				{
					var oldMonitorIndices = Enumerable.Range(0, Monitors.Count).ToList();
					var newMonitorItems = new List<IMonitor>();

					foreach (var item in await MonitorManager.EnumerateMonitorsAsync(TimeSpan.FromSeconds(12)))
					{
						OperationRecorder.AddGroupRecordItem("Items", item.ToString());

						var oldMonitorExists = false;

						foreach (int index in oldMonitorIndices)
						{
							var oldMonitor = Monitors[index];
							if (string.Equals(oldMonitor.DeviceInstanceId, item.DeviceInstanceId, StringComparison.OrdinalIgnoreCase))
							{
								oldMonitorExists = true;
								oldMonitorIndices.Remove(index);
								oldMonitor.Replace(item);
								break;
							}
						}

						if (!oldMonitorExists)
							newMonitorItems.Add(item);
					}

					if (oldMonitorIndices.Count > 0)
					{
						oldMonitorIndices.Reverse(); // Reverse indices to start removing from the tail.
						foreach (int index in oldMonitorIndices)
						{
							DisposeMonitor(Monitors[index]);
							lock (_monitorsLock)
							{
								Monitors.RemoveAt(index);
							}
						}
					}

					if (newMonitorItems.Count > 0)
					{
						foreach (var item in newMonitorItems)
						{
							var newMonitor = GetMonitor(item);
							lock (_monitorsLock)
							{
								Monitors.Add(newMonitor);
							}
						}
					}
				});

				var maxMonitorsCount = await GetMaxMonitorsCountAsync();

				var updateResults = await Task.WhenAll(Monitors
					.Where(x => x.IsReachable)
					.Select((x, index) =>
					{
						if (index < maxMonitorsCount)
						{
							return Task.Run(() =>
							{
								if (x.UpdateBrightness())
								{
									x.IsTarget = true;
								}
								return x.IsControllable;
							});
						}
						x.IsTarget = false;
						return Task.FromResult(false);
					}));

				var controllableMonitorExists = updateResults.Any(x => x);

				foreach (var m in Monitors.Where(x => !x.IsControllable))
					m.IsTarget = !controllableMonitorExists;

				OperationRecorder.AddGroupRecordItems(nameof(Monitors), Monitors.Select(x => x.ToString()));
				await OperationRecorder.EndGroupRecordAsync();

				await intervalTask;
			}
		}
		finally
		{
			if (isEntered)
			{
				ScanningChanged?.Invoke(this, false);

				Interlocked.Exchange(ref _scanCount, 0);
			}
		}
	}

	protected virtual async Task CheckUpdateAsync()
	{
		if (_scanCount > 0)
			return;

		var isEntered = false;
		try
		{
			isEntered = (Interlocked.Increment(ref _updateCount) == 1);
			if (isEntered)
			{
				if (await Task.Run(() => MonitorManager.CheckMonitorsChanged()))
				{
					OnMonitorsChangeFound();
				}
				else
				{
					await Task.WhenAll(Monitors
						.Where(x => x.IsTarget)
						.SelectMany(x => new[]
						{
							Task.Run(() => x.UpdateBrightness()),
							(x.IsContrastChanging ? Task.Run(() => x.UpdateContrast()) : Task.CompletedTask),
						}));
				}
			}
		}
		finally
		{
			if (isEntered)
			{
				Interlocked.Exchange(ref _updateCount, 0);
			}
		}
	}

	protected virtual void Update(string instanceName, int brightness)
	{
		var monitor = !string.IsNullOrEmpty(instanceName)
			? Monitors.FirstOrDefault(x => instanceName.StartsWith(x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase))
			: Monitors.FirstOrDefault(x => x.IsInternal);

		EnsureUnisonWorkable(monitor);
		monitor?.UpdateBrightness(brightness);
	}

	protected virtual async Task UpdateMessageAsync(string deviceInstanceId, string message)
	{
		var monitor = Monitors.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, deviceInstanceId, StringComparison.OrdinalIgnoreCase));
		if (monitor is not null)
		{
			await monitor.AddNormalMessageAsync(message, TimeSpan.FromSeconds(30));
		}
	}

	private void ReflectMouseWheel(int delta)
	{
		var monitor = SelectedMonitor;
		if (monitor is not { IsTarget: true, IsControllable: true })
			return;

		EnsureUnisonWorkable(monitor);

		if (delta > 0)
		{
			monitor.IncrementBrightness(ViewManager.WheelFactor, false);
		}
		else
		{
			monitor.DecrementBrightness(ViewManager.WheelFactor, false);
		}
	}

	protected internal MonitorViewModel SelectedMonitor
	{
		get => _selectedMonitor ??= Monitors.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, Settings.SelectedDeviceInstanceId));
		set
		{
			if ((value is null) || ReferenceEquals(_selectedMonitor, value))
				return;

			_selectedMonitor = value;
			Settings.SelectedDeviceInstanceId = value.DeviceInstanceId;
		}
	}
	private MonitorViewModel _selectedMonitor;

	private void MonitorsDispose()
	{
		foreach (var m in Monitors)
			m.Dispose();
	}

	#endregion

	#region Customization

	protected internal virtual bool TryLoadCustomization(string deviceInstanceId, ref string name, ref bool isUnison, ref byte lowest, ref byte highest)
	{
		if (Settings.MonitorCustomizations.TryGetValue(deviceInstanceId, out MonitorCustomizationItem m)
			&& m.IsValid)
		{
			name = m.Name;
			isUnison = m.IsUnison && Settings.EnablesUnison;
			lowest = m.Lowest;
			highest = m.Highest;
			return true;
		}
		return false;
	}

	protected internal virtual void SaveCustomization(string deviceInstanceId, string name, bool isUnison, byte lowest, byte highest)
	{
		MonitorCustomizationItem m = new(name, isUnison, lowest, highest);
		if (m.IsValid && !m.IsDefault)
		{
			Settings.MonitorCustomizations.Add(deviceInstanceId, m);
		}
		else
		{
			Settings.MonitorCustomizations.Remove(deviceInstanceId);
		}
	}

	private bool _isUnisonWorkable;

	protected virtual void EnsureUnisonWorkable(MonitorViewModel monitor)
	{
		if (_isUnisonWorkable || (monitor is not { IsUnison: true }))
			return;

		_current.Dispatcher.Invoke(() =>
		{
			if (!_current.MainWindow.IsLoaded)
			{
				((MainWindow)_current.MainWindow).ShowUnnoticed();
			}
			_isUnisonWorkable = true;
		});
	}

	#endregion

	#region Arguments

	public Task<string> LoadArgumentsAsync() => _keeper.LoadArgumentsAsync();

	public Task SaveArgumentsAsync(string content) => _keeper.SaveArgumentsAsync(content);

	#endregion

	#region Clean

	protected virtual Task CleanAsync()
	{
		var orphanFilePath = Path.Combine(Path.GetTempPath(), "License.html");
		if (!File.Exists(orphanFilePath))
			return Task.CompletedTask;

		return Task.Run(() => File.Delete(orphanFilePath));
	}

	#endregion
}