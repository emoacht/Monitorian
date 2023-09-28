using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public class MouseDownParentAction : TriggerAction<DependencyObject>
{
	public MouseButton MouseButton
	{
		get { return (MouseButton)GetValue(MouseButtonProperty); }
		set { SetValue(MouseButtonProperty, value); }
	}
	public static readonly DependencyProperty MouseButtonProperty =
		DependencyProperty.Register(
			"MouseButton",
			typeof(MouseButton),
			typeof(MouseDownParentAction),
			new PropertyMetadata(default(MouseButton)));

	private UIElement _parent;

	protected override void Invoke(object parameter)
	{
		var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton) { RoutedEvent = Mouse.MouseDownEvent };
		_parent ??= VisualTreeHelper.GetParent(this.AssociatedObject) as UIElement;
		_parent?.RaiseEvent(args);
	}
}