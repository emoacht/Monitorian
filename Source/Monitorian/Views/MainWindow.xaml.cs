using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Monitorian.ViewModels;

namespace Monitorian.Views
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowMover _mover;

		public MainWindow(MainController controller)
		{
			InitializeComponent();

			this.DataContext = new MainWindowViewModel(this, controller);

			_mover = new MainWindowMover(this, controller.NotifyIconComponent.NotifyIcon);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			WindowPosition.DisableTransitions(this);
			WindowEffect.EnableBackgroundBlur(this);
		}

		#region Ready

		public bool IsReady { get; private set; } = true;

		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);

			if (this.Visibility == Visibility.Visible)
			{
				this.Hide();

				IsReady = false;

				Task.Run(async () =>
				{
					await Task.Delay(TimeSpan.FromSeconds(0.2));
					Dispatcher.Invoke(() => IsReady = true);
				});
			}
		}

		#endregion
	}
}