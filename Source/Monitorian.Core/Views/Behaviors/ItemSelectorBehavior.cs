using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public class ItemSelectorBehavior : ItemBehavior<Selector>
{
	/// <summary>
	/// Window as the default focus scope
	/// </summary>
	private Window _window;

	protected override void OnAttached()
	{
		base.OnAttached();

		this.AssociatedObject.Initialized += OnInitialized;
		this.AssociatedObject.Loaded += OnLoaded;
		this.AssociatedObject.Unloaded += OnUnloaded;
		this.AssociatedObject.GotFocus += OnGotFocus;
		this.AssociatedObject.LostFocus += OnLostFocus;

		_window = Window.GetWindow(this.AssociatedObject);
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();

		this.AssociatedObject.Loaded -= OnLoaded;
		this.AssociatedObject.Unloaded -= OnUnloaded;
		this.AssociatedObject.GotFocus -= OnGotFocus;
		this.AssociatedObject.LostFocus -= OnLostFocus;
		this.AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
	}

	private void OnInitialized(object sender, EventArgs e)
	{
		this.AssociatedObject.Initialized -= OnInitialized;

		Subscribe(typeof(Selector));
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		this.AssociatedObject.Focus();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		Unsubscribe();
		this.Detach();
	}

	private void OnGotFocus(object sender, RoutedEventArgs e)
	{
		if (ReferenceEquals(FocusManager.GetFocusedElement(_window), this.AssociatedObject))
			this.AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		this.AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (0 <= this.AssociatedObject.SelectedIndex)
			return;

		var container = this.AssociatedObject.ItemContainerGenerator.ContainerFromIndex(0);
		if (container is null)
			return;

		e.Handled = true;
		IsByKey = true;
		Selector.SetIsSelected(container, true);
	}
}