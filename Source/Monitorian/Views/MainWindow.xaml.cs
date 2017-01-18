using System;
using System.Collections;
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

using Monitorian.Models;
using Monitorian.ViewModels;
using Monitorian.Views.Movers;

namespace Monitorian.Views
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowMover _mover;

		public MainWindow(MainController controller)
		{
			InitializeComponent();

			this.DataContext = new MainWindowViewModel(controller);

			_mover = new MainWindowMover(this, controller.NotifyIconComponent.NotifyIcon);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			WindowPosition.DisableTransitions(this);
			WindowEffect.EnableBackgroundBlur(this);
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			CheckDefaultHeights();

			BindingOperations.SetBinding(
				this,
				IsLargeElementsProperty,
				new Binding(nameof(Settings.IsLargeElements))
				{
					Source = ((MainWindowViewModel)this.DataContext).Settings,
					Mode = BindingMode.OneWay
				});

			//this.InvalidateProperty(IsLargeElementsProperty);
		}

		protected override void OnClosed(EventArgs e)
		{
			BindingOperations.ClearBinding(
				this,
				IsLargeElementsProperty);

			base.OnClosed(e);
		}

		#region Elements

		private const double ShrinkFactor = 0.6;
		private Dictionary<string, double> _defaultHeights;

		private void CheckDefaultHeights()
		{
			_defaultHeights = this.Resources.Cast<DictionaryEntry>()
				.Where(x => ((string)x.Key).EndsWith("Height", StringComparison.Ordinal))
				.Where(x => (x.Value is double) && ((double)x.Value > 0))
				.ToDictionary(x => (string)x.Key, x => (double)x.Value);
		}

		public bool IsLargeElements
		{
			get { return (bool)GetValue(IsLargeElementsProperty); }
			set { SetValue(IsLargeElementsProperty, value); }
		}
		public static readonly DependencyProperty IsLargeElementsProperty =
			DependencyProperty.Register(
				"IsLargeElements",
				typeof(bool),
				typeof(MainWindow),
				new PropertyMetadata(
					true,
					(d, e) =>
					{
						// Setting the same value will not trigger calling this method.					

						var window = (MainWindow)d;
						if (window._defaultHeights == null)
							return;

						var factor = (bool)e.NewValue ? 1D : ShrinkFactor;

						foreach (var pair in window._defaultHeights)
						{
							window.Resources[pair.Key] = pair.Value * factor;
						}
					}));

		#endregion

		#region Show/Hide

		public bool CanEditNames
		{
			get { return (bool)GetValue(CanEditNamesProperty); }
			set { SetValue(CanEditNamesProperty, value); }
		}
		public static readonly DependencyProperty CanEditNamesProperty =
			DependencyProperty.Register(
				"CanEditNames",
				typeof(bool),
				typeof(MainWindow),
				new PropertyMetadata(false));

		public bool CanBeShown { get; private set; } = true;

		public void Show(bool canEditNames)
		{
			this.CanEditNames = canEditNames;
			this.Show();
		}

		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);

			if (this.Visibility != Visibility.Visible)
				return;

			this.Hide();
			CanEditNames = false;
			CanBeShown = false;

			Task.Run(async () =>
			{
				await Task.Delay(TimeSpan.FromSeconds(0.2));
				this.Dispatcher.Invoke(() => CanBeShown = true);
			});
		}

		#endregion
	}
}