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

		private readonly SettingsWatcher _settingsWatcher;
		private readonly PowerWatcher _powerWatcher;
		private readonly BrightnessWatcher _brightnessWatcher;

		public AppControllerCore(AppKeeper keeper, SettingsCore settings)
		{
			this._keeper = keeper ?? throw new ArgumentNullException(nameof(keeper));
			this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));

			LanguageService.SwitchDefault();

			Monitors = new ObservableCollection<MonitorViewModel>();
			BindingOperations.EnableCollectionSynchronization(Monitors, _monitorsLock);

			NotifyIconContainer = new NotifyIconContainer();

			_settingsWatcher = new SettingsWatcher();
			_powerWatcher = new PowerWatcher();
			_brightnessWatcher = new BrightnessWatcher();
		}

		public virtual async Task InitiateAsync()
		{
			Settings.Initiate();
			Settings.KnownMonitors.AbsoluteCapacity = MaxKnownMonitorsCount;
			Settings.PropertyChanged += OnSettingsChanged;

			NotifyIconContainer.ShowIcon("pack://application:,,,/Monitorian.Core;component/Resources/Icons/TrayIcon.ico", ProductInfo.Title);

			_current.MainWindow = new MainWindow(this);
			_current.MainWindow.Deactivated += (sender, e) => MonitorsResetByKey();

			if (StartupAgent.IsWindowShowExpected())
				_current.MainWindow.Show();

			await ScanAsync();

			StartupAgent.Requested += (sender, e) => e.Response = OnRequested(sender, e.Args);

			NotifyIconContainer.MouseLeftButtonClick += OnMainWindowShowRequestedBySelf;
			NotifyIconContainer.MouseRightButtonClick += OnMenuWindowShowRequested;

			_settingsWatcher.Subscribe(() => OnMonitorsChangeInferred());
			_powerWatcher.Subscribe(() => OnMonitorsChangeInferred());
			_brightnessWatcher.Subscribe((instanceName, brightness) => Update(instanceName, brightness));
		}

		public virtual void End()
		{
			MonitorsDispose();
			NotifyIconContainer.Dispose();

			_settingsWatcher.Dispose();
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

			window.Show();
			window.Activate();
		}

		protected virtual void ShowMenuWindow(Point pivot)
		{
			var window = new MenuWindow(this, pivot);
			window.ViewModel.CloseAppRequested += (sender, e) => _current.Shutdown();
			window.AddMenuItem(new ProbeSection());
			window.Show();
		}

		protected virtual void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Settings.EnablesUnison))
				OnSettingsEnablesUnisonChanged();
		}

		#region Monitors

		protected virtual async void OnMonitorsChangeInferred() => await ScanAsync();

		internal event EventHandler<bool> ScanningChanged;

		protected virtual Task<byte> GetMaxMonitorsCountAsync() => Task.FromResult<byte>(4);
		protected const int MaxKnownMonitorsCount = 64;

		protected virtual MonitorViewModel GetMonitor(IMonitor monitorItem) => new MonitorViewModel(this, monitorItem);
		protected virtual void DisposeMonitor(MonitorViewModel monitor) => monitor?.Dispose();

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
						.Where(x => x.IsControllable)
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

		private void MonitorsResetByKey()
		{
			foreach (var monitor in Monitors)
				monitor.IsByKey = false;
		}

		#endregion

		#region Name/Unison

		private void OnSettingsEnablesUnisonChanged()
		{
			if (Settings.EnablesUnison)
				return;

			foreach (var monitor in Monitors)
				monitor.IsUnison = false;
		}

		protected internal virtual bool TryLoadNameUnison(string deviceInstanceId, ref string name, ref bool isUnison)
		{
			if (Settings.KnownMonitors.TryGetValue(deviceInstanceId, out MonitorValuePack value))
			{
				name = value.Name;
				isUnison = value.IsUnison;
				return true;
			}
			return false;
		}

		protected internal virtual void SaveNameUnison(string deviceInstanceId, string name, bool isUnison)
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
	}
}