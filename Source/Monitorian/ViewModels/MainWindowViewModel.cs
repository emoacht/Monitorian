using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Monitorian.Models;

namespace Monitorian.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly MainController _controller;
		public Settings Settings => _controller.Settings;

		public MainWindowViewModel(MainController controller)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
			this._controller.ScanningChanged += OnScanningChanged;
		}

		public ListCollectionView MonitorsView
		{
			get
			{
				if (_monitorsView is null)
				{
					_monitorsView = new ListCollectionView(_controller.Monitors);
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
	}
}