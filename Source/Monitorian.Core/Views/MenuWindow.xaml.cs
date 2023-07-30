using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views.Controls;
using ScreenFrame;
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
			_mover.ForegroundWindowChanged += OnDeactivated;
			_mover.AppDeactivated += OnDeactivated;

			controller.WindowPainter.Add(this);
		}

		public UIElementCollection HeadSection => this.HeadItems.Children;
		public UIElementCollection MenuSectionTop => this.MenuItemsTop.Children;
		public UIElementCollection MenuSectionMiddle => this.MenuItemsMiddle.Children;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			EnsureFlowDirection(this);
		}

		public static void EnsureFlowDirection(ContentControl rootControl)
		{
			if (!LanguageService.IsResourceRightToLeft)
				return;

			var resourceValues = new HashSet<string>(LanguageService.ResourceDictionary.Values);

			foreach (var itemControl in LogicalTreeHelperAddition.EnumerateDescendants<ContentControl>(rootControl)
				.Select(x => x.Content as ButtonBase)
				.Where(x => x is not null))
			{
				TemplateElement.SetVisibility(itemControl, Visibility.Visible);

				if (resourceValues.Contains(itemControl.Content))
					itemControl.FlowDirection = FlowDirection.RightToLeft;
			}
		}

		#region Show/Close

		public void DepartFromForeground()
		{
			this.Topmost = false;
		}

		public async void ReturnToForeground()
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

		private void OnDeactivated(object sender, EventArgs e)
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