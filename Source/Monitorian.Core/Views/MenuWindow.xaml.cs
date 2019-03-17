using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using ScreenFrame.Movers;

namespace Monitorian.Core.Views
{
	public partial class MenuWindow : Window
	{
		private readonly FloatWindowMover _mover;
		internal MenuWindowViewModel ViewModel => (MenuWindowViewModel)this.DataContext;

		public MenuWindow(AppControllerCore controller, Point pivot)
		{
			LanguageService.Switch();

			InitializeComponent();

			this.DataContext = new MenuWindowViewModel(controller);

			_mover = new FloatWindowMover(this, pivot);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			WindowEffect.EnableBackgroundTranslucency(this);
		}

		public void AddHeadItem(Control item) => this.HeadItems.Children.Add(item);
		public void RemoveHeadItem(Control item) => this.HeadItems.Children.Remove(item);

		public void AddMenuItem(Control item) => this.MenuItems.Children.Insert(0, item);
		public void RemoveMenuItem(Control item) => this.MenuItems.Children.Remove(item);

		#region Close

		private bool _isClosing = false;

		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);

			if (!_isClosing)
				this.Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				_isClosing = true;
				ViewModel.Dispose();
			}

			base.OnClosing(e);
		}

		#endregion
	}
}