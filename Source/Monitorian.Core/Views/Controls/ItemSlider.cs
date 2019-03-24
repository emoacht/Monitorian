using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using ScreenFrame;

namespace Monitorian.Core.Views.Controls
{
	public class ItemSlider : CompoundSlider
	{
		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			SubscribeItemContainer();
		}

		#region Focus

		public bool IsByKey
		{
			get { return (bool)GetValue(IsByKeyProperty); }
			set { SetValue(IsByKeyProperty, value); }
		}
		public static readonly DependencyProperty IsByKeyProperty =
			DependencyProperty.Register(
				"IsByKey",
				typeof(bool),
				typeof(ItemSlider),
				new PropertyMetadata(false));

		public static HashSet<Key> KeysToWindow { get; } = new HashSet<Key>(); // Static property

		private Window _window;
		private UIElement _parent;

		protected override void OnPreviewKeyDown(KeyEventArgs e)
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
					if (KeysToWindow.Contains(e.Key))
					{
						e.Handled = true;
						RedirectToWindow();
					}
					else
					{
						IsByKey = true;

						switch (e.Key)
						{
							case Key.Up:
							case Key.Down:
								e.Handled = true;
								RedirectToParent();
								break;
						}
					}
					break;
			}

			base.OnPreviewKeyDown(e);

			void RedirectToWindow()
			{
				if (_window is null)
					_window = Window.GetWindow(this);

				var args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key) { RoutedEvent = Keyboard.KeyDownEvent };
				_window.RaiseEvent(args);
			}

			void RedirectToParent()
			{
				if (_parent is null)
					_parent = VisualTreeHelper.GetParent(this) as UIElement;

				var args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key) { RoutedEvent = Keyboard.KeyDownEvent };
				_parent.RaiseEvent(args);
			}
		}

		private ListBoxItem _container;

		private void SubscribeItemContainer()
		{
			if (VisualTreeHelperAddition.TryGetAncestor(this, out _container))
			{
				_container.Selected += OnSelected;
				_container.Unselected += OnUnselected;
			}

			void OnSelected(object sender, RoutedEventArgs _)
			{
				((UIElement)sender).Focusable = false;

				if (!this.IsFocused)
					this.Focus();
			}

			void OnUnselected(object sender, RoutedEventArgs _)
			{
				((UIElement)sender).Focusable = true;
			}
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			if ((_container != null) && !_container.IsSelected)
			{
				Selector.SetIsSelected(_container, true);
			}
		}

		#endregion
	}
}