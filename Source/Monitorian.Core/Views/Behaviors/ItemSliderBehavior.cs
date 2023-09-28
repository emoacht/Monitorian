using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public class ItemSliderBehavior : ItemBehavior<Slider>
{
	/// <summary>
	/// Window as the destination to which reserved keys are redirected
	/// </summary>
	private Window _window;

	/// <summary>
	/// Item container which hosts first and second Sliders
	/// </summary>
	private ListBoxItem _container;

	protected override void OnAttached()
	{
		base.OnAttached();

		_window = Window.GetWindow(this.AssociatedObject);
		_container = this.AssociatedObject.GetSelfAndAncestors().OfType<ListBoxItem>().FirstOrDefault();

		this.AssociatedObject.Initialized += OnInitialized;
		this.AssociatedObject.Unloaded += OnUnloaded;
		this.AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

		if (_container is not null)
		{
			this.AssociatedObject.GotFocus += OnGotFocus;

			var expression = BindingOperations.GetBindingExpression(this, IsSelectedProperty);
			if (expression is not null) // IsSelectedProperty is bound.
			{
				this.AssociatedObject.Loaded += OnLoaded;

				_container.Selected += OnContainerSelected;
				_container.Unselected += OnContainerUnselected;
				_container.GotFocus += OnContainerSelected;
			}
		}
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();

		this.AssociatedObject.Unloaded -= OnUnloaded;
		this.AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

		if (_container is not null)
		{
			this.AssociatedObject.Loaded -= OnLoaded;
			this.AssociatedObject.GotFocus -= OnGotFocus;

			_container.Selected -= OnContainerSelected;
			_container.Unselected -= OnContainerUnselected;
			_container.GotFocus -= OnContainerSelected;
		}
	}

	private void OnInitialized(object sender, EventArgs e)
	{
		this.AssociatedObject.Initialized -= OnInitialized;

		Subscribe(typeof(Selector));
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		this.AssociatedObject.Loaded -= OnLoaded;

		if (IsSelected)
			OnContainerSelected(null, new RoutedEventArgs());
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		this.AssociatedObject.Unloaded -= OnUnloaded;

		Unsubscribe();
		this.Detach();
	}

	private void OnGotFocus(object sender, RoutedEventArgs e)
	{
		if (!_container.IsSelected)
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

							if (SecondObject is { Visibility: Visibility.Visible })
							{
								try
								{
									this.AssociatedObject.Focusable = false;
									SecondObject.Focus();
								}
								finally
								{
									this.AssociatedObject.Focusable = true;
								}
							}
							else
							{
								if (!this.AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down)))
								{
									UIElement element = this.AssociatedObject;
									while (element.PredictFocus(FocusNavigationDirection.Up) is UIElement buffer)
										element = buffer;

									if (!ReferenceEquals(element, this.AssociatedObject))
										element.Focus();
								}
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
			new PropertyMetadata(defaultValue: false));

	private void OnContainerSelected(object sender, RoutedEventArgs e)
	{
		if ((SecondObject is not null) && ReferenceEquals(e.OriginalSource, SecondObject))
		{
			_container.Focusable = false;
			SecondObject.Focus();
		}
		else if (!this.AssociatedObject.IsFocused)
		{
			_container.Focusable = false;
			this.AssociatedObject.Focus();
		}
	}

	private void OnContainerUnselected(object sender, RoutedEventArgs e)
	{
		_container.Focusable = true;
	}

	public Slider SecondObject
	{
		get { return (Slider)GetValue(SecondObjectProperty); }
		set { SetValue(SecondObjectProperty, value); }
	}
	public static readonly DependencyProperty SecondObjectProperty =
		DependencyProperty.Register(
			"SecondObject",
			typeof(Slider),
			typeof(ItemSliderBehavior),
			new PropertyMetadata(defaultValue: null));
}