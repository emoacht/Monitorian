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

using Monitorian.Core.Helper;
using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views.Controls;
using Monitorian.Core.Views.Touchpad;
using ScreenFrame.Movers;

namespace Monitorian.Core.Views
{
	public partial class MainWindow : Window
	{
		private readonly StickWindowMover _mover;
		private readonly TouchpadTracker _tracker;
		public MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

		public MainWindow(AppControllerCore controller)
		{
			LanguageService.Switch();

			InitializeComponent();

			this.DataContext = new MainWindowViewModel(controller);

			_mover = new StickWindowMover(this, controller.NotifyIconContainer.NotifyIcon);

			_tracker = new TouchpadTracker(this);
			_tracker.ManipulationDelta += (_, delta) =>
			{
				var slider = FocusManager.GetFocusedElement(this) as EnhancedSlider;
				slider?.ChangeValue(delta);
			};
			_tracker.ManipulationCompleted += (_, _) =>
			{
				var slider = FocusManager.GetFocusedElement(this) as EnhancedSlider;
				slider?.EnsureUpdateSource();
			};
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			WindowEffect.DisableTransitions(this);
			WindowEffect.EnableBackgroundTranslucency(this);
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			CheckDefaultHeights();

			BindingOperations.SetBinding(
				this,
				UsesLargeElementsProperty,
				new Binding(nameof(SettingsCore.UsesLargeElements))
				{
					Source = ((MainWindowViewModel)this.DataContext).Settings,
					Mode = BindingMode.OneWay
				});

			//this.InvalidateProperty(UsesLargeElementsProperty);
		}

		protected override void OnClosed(EventArgs e)
		{
			BindingOperations.ClearBinding(
				this,
				UsesLargeElementsProperty);

			base.OnClosed(e);
		}

		#region Elements

		private const double ShrinkFactor = 0.64;
		private Dictionary<string, double> _defaultHeights;
		private const string SliderHeightName = "SliderHeight";

		private void CheckDefaultHeights()
		{
			_defaultHeights = this.Resources.Cast<DictionaryEntry>()
				.Where(x => (x.Key is string key) && key.EndsWith("Height", StringComparison.Ordinal))
				.Where(x => x.Value is double height and > 0D)
				.ToDictionary(x => (string)x.Key, x => (double)x.Value);
		}

		public bool UsesLargeElements
		{
			get { return (bool)GetValue(UsesLargeElementsProperty); }
			set { SetValue(UsesLargeElementsProperty, value); }
		}
		public static readonly DependencyProperty UsesLargeElementsProperty =
			DependencyProperty.Register(
				"UsesLargeElements",
				typeof(bool),
				typeof(MainWindow),
				new PropertyMetadata(
					true,
					(d, e) =>
					{
						// Setting the same value will not trigger calling this method.					

						var window = (MainWindow)d;
						if (window._defaultHeights is null)
							return;

						var factor = (bool)e.NewValue ? 1D : ShrinkFactor;

						foreach (var (key, value) in window._defaultHeights)
						{
							var buffer = value * factor;
							if (key == SliderHeightName)
								buffer = Math.Ceiling(buffer / 4) * 4;

							window.Resources[key] = buffer;
						}
					}));

		#endregion

		#region Show/Hide

		public bool IsForeground => _mover.IsForeground();

		public void ShowForeground()
		{
			try
			{
				this.Topmost = true;

				// When window is deactivated, a focused element will lose focus and usually,
				// no element has focus until window is activated again and the last focused element
				// will automatically get focus back. Therefore, in usual case, no focused element
				// exists before Window.Show method. However, during window is not active, it is
				// possible to set focus on an element and such focused element is found here.
				// The issue is that such focused element will lose focus because the element which
				// had focus before window was deactivated will restore focus even though any other
				// element has focus. To prevent this unintended change of focus, it is necessary
				// to set focus back on the element which had focus before Window.Show method.
				var currentFocusedElement = FocusManager.GetFocusedElement(this);

				base.Show();

				if (currentFocusedElement is not null)
				{
					var restoredFocusedElement = FocusManager.GetFocusedElement(this);
					if (restoredFocusedElement != currentFocusedElement)
						FocusManager.SetFocusedElement(this, currentFocusedElement);
				}
			}
			finally
			{
				this.Topmost = false;
			}
		}

		public bool CanBeShown => (_preventionTime < DateTimeOffset.Now);
		private DateTimeOffset _preventionTime;

		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);

			if (this.Visibility != Visibility.Visible)
				return;

			// Set time to prevent this window from being shown unintentionally. 
			_preventionTime = DateTimeOffset.Now + TimeSpan.FromSeconds(0.2);

			ClearHide();
		}

		public async void ClearHide()
		{
			// Clear focus.
			FocusManager.SetFocusedElement(this, null);

			// Wait for this window to be refreshed before being hidden.
			await Task.Delay(TimeSpan.FromSeconds(0.1));

			this.Hide();
		}

		#endregion
	}
}