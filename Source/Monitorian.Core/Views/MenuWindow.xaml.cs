﻿using System;
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
		public MenuWindowViewModel ViewModel => (MenuWindowViewModel)this.DataContext;

		public MenuWindow(AppControllerCore controller, Point pivot)
		{
			LanguageService.Switch();

			InitializeComponent();

			this.DataContext = new MenuWindowViewModel(controller);

			_mover = new FloatWindowMover(this, pivot);
			_mover.AppDeactivated += OnCloseTriggered;
			_mover.EscapeKeyDown += OnCloseTriggered;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			WindowEffect.EnableBackgroundTranslucency(this);
		}

		public UIElementCollection HeadSection => this.HeadItems.Children;
		public UIElementCollection MenuSectionTop => this.MenuItemsTop.Children;
		public UIElementCollection MenuSectionMiddle => this.MenuItemsMiddle.Children;

		#region Show/Close

		public void DepartFromForegrond()
		{
			this.Topmost = false;
		}

		public async void ReturnToForegroud()
		{
			// Wait for this window to be able to be activated.
			await Task.Delay(TimeSpan.FromMilliseconds(100));

			if (_isClosing)
				return;

			// Activate this window. This is necessary to assure this window is foreground.
			this.Activate();

			this.Topmost = true;
		}

		private bool _isClosing = false;

		private void OnCloseTriggered(object sender, EventArgs e)
		{
			if (!_isClosing && this.IsLoaded)
				this.Close();
		}

		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);

			if (!this.Topmost)
				return;

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