using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

using Monitorian.Models;
using Monitorian.Models.Monitor;
using Monitorian.Models.Watcher;
using Monitorian.ViewModels;
using Monitorian.Views;
using ScreenFrame;
using StartupAgency;

namespace Monitorian
{
	public class MainController
	{
		private readonly Application _current = App.Current;

		public Settings Settings { get; }
		internal StartupAgent StartupAgent { get; }

		public ObservableCollection<MonitorViewModel> Monitors { get; }
		private readonly object _monitorsLock = new object();

		public NotifyIconContainer NotifyIconContainer { get; }

		private readonly SettingsWatcher _settingsWatcher;
		private readonly PowerWatcher _powerWatcher;
		private readonly BrightnessWatcher _brightnessWatcher;

		public MainController(StartupAgent agent)
		{
			Settings = new Settings();
			StartupAgent = agent ?? throw new ArgumentNullException(nameof(agent));

			LanguageService.SwitchDefault();

			Monitors = new ObservableCollection<MonitorViewModel>();
			BindingOperations.EnableCollectionSynchronization(Monitors, _monitorsLock);

			NotifyIconContainer = new NotifyIconContainer();
			NotifyIconContainer.MouseLeftButtonClick += OnMainWindowShowRequestedBySelf;
			NotifyIconContainer.MouseRightButtonClick += OnMenuWindowShowRequested;

			_settingsWatcher = new SettingsWatcher();
			_powerWatcher = new PowerWatcher();
			_brightnessWatcher = new BrightnessWatcher();
		}

		internal async Task InitiateAsync()
		{
			Settings.Initiate();
			Settings.KnownMonitors.AbsoluteCapacity = _maxNameCount.Value;
			Settings.PropertyChanged += OnSettingsChanged;

			NotifyIconContainer.ShowIcon("pack://application:,,,/Resources/Icons/TrayIcon.ico", ProductInfo.Title);

			_current.MainWindow = new MainWindow(this);

			if (!StartupAgent.IsStartedOnSignIn())
				_current.MainWindow.Show();

			StartupAgent.Requested += OnMainWindowShowRequestedByOther;

			await ScanAsync();

			_settingsWatcher.Subscribe(() => ScanAsync());
			_powerWatcher.Subscribe(() => ScanAsync());
			_brightnessWatcher.Subscribe((instanceName, brightness) => Update(instanceName, brightness));
		}

		internal void End()
		{
			MonitorsDispose();
			NotifyIconContainer.Dispose();

			_settingsWatcher.Dispose();
			_powerWatcher.Dispose();
			_brightnessWatcher.Dispose();
		}

		private async void OnMainWindowShowRequestedBySelf(object sender, EventArgs e)
		{
			ShowMainWindow();
			await UpdateAsync();
		}

		private async void OnMainWindowShowRequestedByOther(object sender, EventArgs e)
		{
			_current.Dispatcher.Invoke(() => ShowMainWindow());
			await ScanAsync();
		}

		private void OnMenuWindowShowRequested(object sender, Point e)
		{
			ShowMenuWindow(e);
		}

		private void ShowMainWindow()
		{
			var window = (MainWindow)_current.MainWindow;
			if (!window.CanBeShown)
				return;

			if (window.Visibility == Visibility.Visible)
				return;

			window.Show();
			window.Activate();
		}

		private void ShowMenuWindow(Point pivot)
		{
			var window = new MenuWindow(this, pivot);
			window.ViewModel.CloseAppRequested += (sender, e) => _current.Shutdown();
			window.AddMenuItem(new Probe());
			window.Show();
		}

		private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Settings.EnablesUnison))
				OnSettingsEnablesUnisonChanged();
		}

		#region Monitors

		internal event EventHandler<bool> ScanningChanged;

		private static readonly Lazy<int> _maxMonitorCount = new Lazy<int>(() =>
		{
			int count = 4;
			SetCount(ref count);
			return count;
		});

		private static readonly Lazy<int> _maxNameCount = new Lazy<int>(() => _maxMonitorCount.Value * 4);

		private int _scanCount = 0;
		private int _updateCount = 0;

		private async Task ScanAsync()
		{
			var isEntered = false;
			try
			{
				isEntered = (Interlocked.Increment(ref _scanCount) == 1);
				if (isEntered)
				{
					ScanningChanged?.Invoke(this, true);

					await Task.Run(async () =>
					{
						var oldMonitorIndices = Enumerable.Range(0, Monitors.Count).ToList();
						var newMonitorItems = new List<IMonitor>();

						foreach (var item in await MonitorManager.EnumerateMonitorsAsync())
						{
							var oldMonitorExists = false;

							foreach (int index in oldMonitorIndices)
							{
								var oldMonitor = Monitors[index];
								if (string.Equals(oldMonitor.DeviceInstanceId, item.DeviceInstanceId, StringComparison.OrdinalIgnoreCase)
									&& (oldMonitor.IsAccessible == item.IsAccessible))
								{
									oldMonitorExists = true;
									oldMonitorIndices.Remove(index);
									item.Dispose();
									break;
								}
							}

							if (!oldMonitorExists)
								newMonitorItems.Add(item);
						}

						if (oldMonitorIndices.Count > 0)
						{
							oldMonitorIndices.Reverse(); // Reverse indices to start removing from the tail.
							foreach (var index in oldMonitorIndices)
							{
								Monitors[index].Dispose();
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
								var newMonitor = new MonitorViewModel(this, item);
								lock (_monitorsLock)
								{
									Monitors.Add(newMonitor);
								}
							}
						}
					});

					var controllableMonitorExists = false;

					await Task.WhenAll(Monitors
						.Where(x => x.IsControllable)
						.Select((x, index) =>
						{
							controllableMonitorExists = true;

							if (index < _maxMonitorCount.Value)
							{
								return Task.Run(() =>
								{
									x.UpdateBrightness();
									x.IsTarget = true;
								});
							}
							else
							{
								x.IsTarget = false;
								return Task.CompletedTask;
							}
						}));

					Monitors
						.Where(x => !x.IsControllable)
						.ToList()
						.ForEach(x => x.IsTarget = !controllableMonitorExists);
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

		private async Task UpdateAsync()
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

		private void Update(string instanceName, int brightness)
		{
			var monitor = Monitors.FirstOrDefault(x => instanceName.StartsWith(x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
			monitor?.UpdateBrightness(brightness);
		}

		private void MonitorsDispose()
		{
			foreach (var monitor in Monitors)
				monitor.Dispose();
		}

		#endregion

		#region Name & Unison

		private void OnSettingsEnablesUnisonChanged()
		{
			if (Settings.EnablesUnison)
				return;

			Monitors
				.Where(x => x.IsUnison)
				.ToList()
				.ForEach(x => x.IsUnison = false);
		}

		internal bool TryLoadNameUnison(string deviceInstanceId, ref string name, ref bool isUnison)
		{
			if (Settings.KnownMonitors.TryGetValue(deviceInstanceId, out MonitorValuePack value))
			{
				name = value.Name;
				isUnison = value.IsUnison;
				return true;
			}
			else
			{
				return false;
			}
		}

		internal void SaveNameUnison(string deviceInstanceId, string name, bool isUnison)
		{
			if ((name != null) || isUnison)
			{
				Settings.KnownMonitors.Add(deviceInstanceId, new MonitorValuePack(name, isUnison));
			}
			else
			{
				Settings.KnownMonitors.Remove(deviceInstanceId);
			}
		}

		#endregion

		#region Configuration

		[Conditional("UNLIMITED")]
		private static void SetCount(ref int count)
		{
			count *= 8;
		}

		#endregion
	}
}