using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;
using Monitorian.Core.Models.Watcher;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views;
using ScreenFrame;
using StartupAgency;

namespace Monitorian.Core
{
	public class AppControllerCore
	{
		protected readonly Application _current = Application.Current;

		protected readonly AppKeeper _keeper;
		protected internal StartupAgent StartupAgent => _keeper.StartupAgent;

		protected internal SettingsCore Settings { get; }

		public ObservableCollection<MonitorViewModel> Monitors { get; }
		protected readonly object _monitorsLock = new object();

		public NotifyIconContainer NotifyIconContainer { get; }

		private readonly DisplayWatcher _displayWatcher;
		private readonly PowerWatcher _powerWatcher;
		private readonly BrightnessWatcher _brightnessWatcher;

		protected OperationRecorder Recorder { get; private set; }

		public AppControllerCore(AppKeeper keeper, SettingsCore settings)
		{
			this._keeper = keeper ?? throw new ArgumentNullException(nameof(keeper));
			this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));

			LanguageService.SwitchDefault();

			Monitors = new ObservableCollection<MonitorViewModel>();
			BindingOperations.EnableCollectionSynchronization(Monitors, _monitorsLock);

			NotifyIconContainer = new NotifyIconContainer();

			_displayWatcher = new DisplayWatcher();
			_powerWatcher = new PowerWatcher();
			_brightnessWatcher = new BrightnessWatcher();
		}

		public virtual async Task InitiateAsync()
		{
			Settings.Initiate();
			Settings.MonitorCustomizations.AbsoluteCapacity = MaxKnownMonitorsCount;
			Settings.PropertyChanged += OnSettingsChanged;

			OnSettingsInitiated();

			NotifyIconContainer.ShowIcon("pack://application:,,,/Monitorian.Core;component/Resources/Icons/TrayIcon.ico", ProductInfo.Title);

			_current.MainWindow = new MainWindow(this);
			_current.MainWindow.Deactivated += (sender, e) => MonitorsResetByKey();

			if (StartupAgent.IsWindowShowExpected())
				_current.MainWindow.Show();

			await ScanAsync();

			StartupAgent.Requested += (sender, e) => e.Response = OnRequested(sender, e.Args);

			NotifyIconContainer.MouseLeftButtonClick += OnMainWindowShowRequestedBySelf;
			NotifyIconContainer.MouseRightButtonClick += OnMenuWindowShowRequested;

			_displayWatcher.Subscribe(() => OnMonitorsChangeInferred(nameof(DisplayWatcher)));
			_powerWatcher.Subscribe((e) => OnMonitorsChangeInferred(nameof(PowerWatcher), e.Mode, e.Count), PowerManagement.GetOnPowerSettingChanged());
			_brightnessWatcher.Subscribe((instanceName, brightness) => Update(instanceName, brightness));
		}

		public virtual void End()
		{
			MonitorsDispose();
			NotifyIconContainer.Dispose();

			_displayWatcher.Dispose();
			_powerWatcher.Dispose();
			_brightnessWatcher.Dispose();
		}

		protected virtual object OnRequested(object sender, IReadOnlyCollection<string> args)
		{
			OnMainWindowShowRequestedByOther(sender, EventArgs.Empty);
			return null;
		}

		protected async void OnMainWindowShowRequestedBySelf(object sender, EventArgs e)
		{
			ShowMainWindow();
			await UpdateAsync();
		}

		protected void OnMainWindowShowRequestedByOther(object sender, EventArgs e)
		{
			_current.Dispatcher.Invoke(() => ShowMainWindow());
			OnMonitorsChangeInferred();
		}

		protected void OnMenuWindowShowRequested(object sender, Point e)
		{
			ShowMenuWindow(e);
		}

		protected virtual void ShowMainWindow()
		{
			var window = (MainWindow)_current.MainWindow;
			if (!window.CanBeShown)
				return;

			if ((window.Visibility == Visibility.Visible) && window.IsForeground)
				return;

			window.ShowForeground();
			window.Activate();
		}

		protected virtual void HideMainWindow()
		{
			((MainWindow)_current.MainWindow).ClearHide();
		}

		protected virtual void ShowMenuWindow(Point pivot)
		{
			var window = new MenuWindow(this, pivot);
			window.ViewModel.CloseAppRequested += (sender, e) => _current.Shutdown();
			window.AddMenuItem(new ProbeSection(this));
			window.Show();
		}

		protected virtual void OnSettingsInitiated()
		{
			if (Settings.MakesOperationLog)
				Recorder = new("Initiated");
		}

		protected virtual void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(Settings.EnablesUnison):
					OnSettingsEnablesUnisonChanged();
					break;

				case nameof(Settings.MakesOperationLog):
					Recorder = Settings.MakesOperationLog ? new("Enabled") : null;
					break;
			}
		}

		#region Monitors

		protected virtual async void OnMonitorsChangeInferred(object sender = null, PowerModes mode = default, int? count = null)
		{
			Recorder?.Record($"{nameof(OnMonitorsChangeInferred)} ({sender}{(mode == default ? string.Empty : $"- {mode} {count}")})");

			if (count == 0)
				return;

			await ScanAsync(TimeSpan.FromSeconds(3));
		}

		protected internal virtual void OnMonitorDetached()
		{
			_displayWatcher.RaiseDisplaySettingsChanged();
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

					Recorder?.StartGroupRecord($"{nameof(ScanAsync)} [{DateTime.Now}]");

					await Task.Run(async () =>
					{
						var oldMonitorIndices = Enumerable.Range(0, Monitors.Count).ToList();
						var newMonitorItems = new List<IMonitor>();

						foreach (var item in await MonitorManager.EnumerateMonitorsAsync())
						{
							Recorder?.AddGroupRecordItem("Items", item.ToString());

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
						.Where(x => x.IsLikelyControllable)
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

					Recorder?.AddGroupRecordItems(nameof(Monitors), Monitors.Select(x => x.ToString()));
					await (Recorder?.EndGroupRecordAsync() ?? Task.CompletedTask);

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

		protected virtual async Task UpdateAsync()
		{
			if (_scanCount > 0)
				return;

			var isEntered = false;
			try
			{
				isEntered = (Interlocked.Increment(ref _updateCount) == 1);
				if (isEntered)
				{
					await Task.WhenAll(Monitors
						.Where(x => x.IsTarget)
						.Select(x => Task.Run(() => x.UpdateBrightness())));
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
			var monitor = Monitors.FirstOrDefault(x => instanceName.StartsWith(x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
			monitor?.UpdateBrightness(brightness);
		}

		private void MonitorsDispose()
		{
			foreach (var m in Monitors)
				m.Dispose();
		}

		private void MonitorsResetByKey()
		{
			var monitor = Monitors.FirstOrDefault(x => x.IsSelectedByKey);

			if (monitor is not null)
				monitor.IsByKey = false;
		}

		protected MonitorViewModel SelectedMonitor { get; private set; }

		protected internal virtual void SaveMonitorUserChanged(MonitorViewModel monitor)
		{
			if ((monitor is null) || ReferenceEquals(SelectedMonitor, monitor))
				return;

			SelectedMonitor = monitor;
			Settings.SelectedDeviceInstanceId = monitor.DeviceInstanceId;
		}

		#endregion

		#region Customization

		private void OnSettingsEnablesUnisonChanged()
		{
			if (Settings.EnablesUnison)
				return;

			foreach (var m in Monitors)
				m.IsUnison = false;
		}

		protected internal virtual bool TryLoadCustomization(string deviceInstanceId, ref string name, ref bool isUnison, ref byte lowest, ref byte highest)
		{
			if (Settings.MonitorCustomizations.TryGetValue(deviceInstanceId, out MonitorCustomizationItem m)
				&& (m.Lowest < m.Highest) && (m.Highest <= 100))
			{
				name = m.Name;
				isUnison = m.IsUnison;
				lowest = m.Lowest;
				highest = m.Highest;
				return true;
			}
			return false;
		}

		protected internal virtual void SaveCustomization(string deviceInstanceId, string name, bool isUnison, byte lowest, byte highest)
		{
			if (((name is not null) || isUnison || (0 != lowest) || (highest != 100))
				&& (lowest < highest) && (highest <= 100))
			{
				Settings.MonitorCustomizations.Add(deviceInstanceId, new MonitorCustomizationItem(name, isUnison, lowest, highest));

			}
			else
			{
				Settings.MonitorCustomizations.Remove(deviceInstanceId);
			}
		}

		#endregion
	}
}