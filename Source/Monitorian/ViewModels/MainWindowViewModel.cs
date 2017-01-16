using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Monitorian.Models.Monitor;
using Monitorian.Views;

namespace Monitorian.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly MainWindow _window;
		private readonly MainController _controller;

		public MainWindowViewModel(MainWindow window, MainController controller)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));
			if (controller == null)
				throw new ArgumentNullException(nameof(controller));

			this._window = window;
			this._controller = controller;
		}

		public ListCollectionView MonitorsView
		{
			get
			{
				if (_monitorsView == null)
				{
					_monitorsView = new ListCollectionView(_controller.Monitors);
					_monitorsView.SortDescriptions.Add(new SortDescription(nameof(IMonitor.DisplayIndex), ListSortDirection.Ascending));
					_monitorsView.SortDescriptions.Add(new SortDescription(nameof(IMonitor.MonitorIndex), ListSortDirection.Ascending));

					((INotifyCollectionChanged)_monitorsView).CollectionChanged += OnCollectionChanged;
				}
				return _monitorsView;
			}
		}
		private ListCollectionView _monitorsView;

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			RaisePropertyChanged(nameof(IsMonitorsEmpty));
		}

		public bool IsMonitorsEmpty => MonitorsView.IsEmpty;
	}
}