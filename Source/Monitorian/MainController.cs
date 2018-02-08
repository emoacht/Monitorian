using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

			Monitors = new ObservableCollection<MonitorViewModel>();
			BindingOperations.EnableCollectionSynchronization(Monitors, _monitorsLock);

			NotifyIconContainer = new NotifyIconContainer();
			NotifyIconContainer.MouseLeftButtonClick += OnMainWindowShowRequested;
			NotifyIconContainer.MouseRightButtonClick += OnMenuWindowShowRequested;

			_settingsWatcher = new SettingsWatcher();
			_powerWatcher = new PowerWatcher();
			_brightnessWatcher = new BrightnessWatcher();
		}

		internal async Task InitiateAsync()
		{
			Settings.Initiate();
			Settings.KnownMonitors.AbsoluteCapacity = _maxNameCount.Value;

			NotifyIconContainer.ShowIcon("pack://application:,,,/Resources/Icons/TrayIcon.ico", ProductInfo.Title);

			_current.MainWindow = new MainWindow(this);

			if (!StartupAgent.IsStartedOnSignIn())
				_current.MainWindow.Show();

			StartupAgent.ShowRequested += OnMainWindowShowRequested;

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

		private async void OnMainWindowShowRequested(object sender, EventArgs e)
		{
			_current.Dispatcher.Invoke(() => ShowMainWindow());
			await UpdateAsync();
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
			window.Show();
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

					var scanTime = DateTimeOffset.Now;
					int accessibleMonitorCount = 0;

					await Task.Run(() =>
					{
						var oldMonitorIndices = Enumerable.Range(0, Monitors.Count).ToList();
						var newMonitorItems = new List<IMonitor>();

						foreach (var item in MonitorManager.EnumerateMonitors())
						{
							var isExisting = false;

							foreach (int index in oldMonitorIndices)
							{
								var oldMonitor = Monitors[index];
								if (string.Equals(oldMonitor.DeviceInstanceId, item.DeviceInstanceId, StringComparison.OrdinalIgnoreCase)
									&& (oldMonitor.IsAccessible == item.IsAccessible))
								{
									isExisting = true;
									oldMonitorIndices.Remove(index);
									item.Dispose();
									break;
								}
							}

							if (!isExisting)
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
								if (newMonitor.IsControllable && (Monitors.Count < _maxMonitorCount.Value))
								{
									newMonitor.UpdateBrightness();
									newMonitor.IsTarget = true;
									accessibleMonitorCount++;
								}
								lock (_monitorsLock)
								{
									Monitors.Add(newMonitor);
								}
							}
						}
					});

					await Task.WhenAll(Monitors
						.Take(_maxMonitorCount.Value)
						.Where(x => x.IsControllable && (x.UpdateTime < scanTime))
						.Select(x => Task.Run(() =>
						{
							x.UpdateBrightness();
							x.IsTarget = true;
							Interlocked.Increment(ref accessibleMonitorCount);
						})));

					var accessibleMonitorExists = (accessibleMonitorCount > 0);
					Monitors
						.Where(x => !x.IsControllable)
						.ToList()
						.ForEach(x => x.IsTarget = !accessibleMonitorExists);
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
			if (monitor != null)
			{
				monitor.UpdateBrightness(brightness);
			}
		}

		private void MonitorsDispose()
		{
			foreach (var monitor in Monitors)
				monitor.Dispose();
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