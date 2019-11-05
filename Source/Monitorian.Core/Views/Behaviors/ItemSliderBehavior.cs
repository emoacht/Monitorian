using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors
{
	public class ItemSliderBehavior : ItemBehavior<Slider>
	{
		/// <summary>
		/// Window as the destination to which reserved keys are redirected
		/// </summary>
		private Window _window;

		private ListBoxItem _container;

		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Initialized += OnInitialized;
			this.AssociatedObject.Loaded += OnLoaded;
			this.AssociatedObject.Unloaded += OnUnloaded;
			this.AssociatedObject.GotFocus += OnGotFocus;
			this.AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

			_window = Window.GetWindow(this.AssociatedObject);

			_container = this.AssociatedObject.GetSelfAndAncestors().OfType<ListBoxItem>().FirstOrDefault();
			if (_container != null)
			{
				_container.Selected += OnSelected;
				_container.Unselected += OnUnselected;
				_container.GotFocus += OnSelected;
			}
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.Loaded -= OnLoaded;
			this.AssociatedObject.Unloaded -= OnUnloaded;
			this.AssociatedObject.GotFocus -= OnGotFocus;
			this.AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

			if (_container != null)
			{
				_container.Selected -= OnSelected;
				_container.Unselected -= OnUnselected;
				_container.GotFocus -= OnSelected;
			}
		}

		private void OnInitialized(object sender, EventArgs e)
		{
			this.AssociatedObject.Initialized -= OnInitialized;

			Subscribe(typeof(Selector));
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (IsSelected && (_container != null))
				OnSelected(null, null);
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			Unsubscribe();
			this.Detach();
		}

		private void OnGotFocus(object sender, RoutedEventArgs e)
		{
			if ((_container != null) && !_container.IsSelected)
				Selector.SetIsSelected(_container, true);
		}

		public static HashSet<Key> ReservedKeys { get; } = new HashSet<Key>(); // Static property

		private void OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Left:
				case Key.Right:
				case Key.Up:
				case Key.Down:
				case Key.PageUp:
				case Key.PageDown:
				case Key.Home:
				case Key.End:
				case Key.Tab:
					if (ReservedKeys.Contains(e.Key))
					{
						e.Handled = true;
						var args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key) { RoutedEvent = Keyboard.KeyDownEvent };
						_window.RaiseEvent(args);
					}
					else
					{
						IsByKey = true;

						switch (e.Key)
						{
							case Key.Up:
								e.Handled = true;
								this.AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
								break;

							case Key.Down:
								e.Handled = true;
								this.AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
								break;

							case Key.Tab:
								e.Handled = true;
								if (!this.AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down)))
								{
									UIElement element = this.AssociatedObject;
									while (element.PredictFocus(FocusNavigationDirection.Up) is UIElement buffer)
										element = buffer;

									if (!ReferenceEquals(element, this.AssociatedObject))
										element.Focus();
								}
								break;
						}
					}
					break;
			}
		}

		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
			set { SetValue(IsSelectedProperty, value); }
		}
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(
				"IsSelected",
				typeof(bool),
				typeof(ItemSliderBehavior),
				new PropertyMetadata(false));

		private void OnSelected(object sender, RoutedEventArgs e)
		{
			if (!this.AssociatedObject.IsFocused)
			{
				_container.Focusable = false;
				this.AssociatedObject.Focus();
			}
		}

		private void OnUnselected(object sender, RoutedEventArgs e)
		{
			_container.Focusable = true;
		}
	}
}