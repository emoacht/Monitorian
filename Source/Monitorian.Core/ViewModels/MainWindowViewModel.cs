using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Monitorian.Core.Models;

namespace Monitorian.Core.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly AppControllerCore _controller;
		public SettingsCore Settings => _controller.Settings;

		public MainWindowViewModel(AppControllerCore controller)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
			this._controller.ScanningChanged += OnScanningChanged;
			Settings.PropertyChanged += OnSettingsChanged;
		}

		public ListCollectionView MonitorsView
		{
			get
			{
				if (_monitorsView is null)
				{
					_monitorsView = new ListCollectionView(_controller.Monitors);
					if (Settings.OrdersArrangement)
					{
						_monitorsView.SortDescriptions.Add(new SortDescription(nameof(MonitorViewModel.MonitorTop), ListSortDirection.Ascending));
					}
					_monitorsView.SortDescriptions.Add(new SortDescription(nameof(MonitorViewModel.DisplayIndex), ListSortDirection.Ascending));
					_monitorsView.SortDescriptions.Add(new SortDescription(nameof(MonitorViewModel.MonitorIndex), ListSortDirection.Ascending));
					_monitorsView.Filter = x => ((MonitorViewModel)x).IsTarget;
					_monitorsView.IsLiveFiltering = true;
					_monitorsView.LiveFilteringProperties.Add(nameof(MonitorViewModel.IsTarget));

					((INotifyCollectionChanged)_monitorsView).CollectionChanged += OnCollectionChanged;
				}
				return _monitorsView;
			}
		}
		private ListCollectionView _monitorsView;

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				//case NotifyCollectionChangedAction.Reset:
				case NotifyCollectionChangedAction.Add:
				case NotifyCollectionChangedAction.Remove:
					RaisePropertyChanged(nameof(IsMonitorsEmpty));

					if (MonitorsView.CurrentItem is null)
					{
						// CollectionView.CurrentItem is automatically synchronized with SelectedItem
						// when the target is an ItemsControl. However, this synchronization is not
						// always fast enough to check if any item is currently selected.
						if (!MonitorsView.Cast<MonitorViewModel>().Any(x => x.IsSelected))
						{
							var monitor = MonitorsView.Cast<MonitorViewModel>()
								.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, Settings.SelectedDeviceInstanceId));
							if (monitor is not null)
								monitor.IsSelected = true;
						}
					}
					break;
			}
		}

		public bool IsMonitorsEmpty => MonitorsView.IsEmpty;

		private void OnScanningChanged(object sender, bool e)
		{
			IsScanning = e;
			RaisePropertyChanged(nameof(IsScanning));
		}

		public bool IsScanning { get; private set; }

		private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(Settings.OrdersArrangement):
					var orderDescription = MonitorsView.SortDescriptions.FirstOrDefault(x => x.PropertyName == nameof(MonitorViewModel.MonitorTop));

					if (Settings.OrdersArrangement)
					{
						if (orderDescription != default)
							return;

						MonitorsView.SortDescriptions.Insert(0, new SortDescription(nameof(MonitorViewModel.MonitorTop), ListSortDirection.Ascending));
					}
					else
					{
						if (orderDescription == default)
							return;

						MonitorsView.SortDescriptions.Remove(orderDescription);
					}

					MonitorsView.Refresh();
					break;
			}
		}

		internal void Deactivate()
		{
			var monitor = MonitorsView.Cast<MonitorViewModel>().FirstOrDefault(x => x.IsSelectedByKey);
			if (monitor is not null)
				monitor.IsByKey = false;
		}
	}
}