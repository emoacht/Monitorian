using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public class FocusElementAction : TriggerAction<DependencyObject>
{
	public UIElement TargetElement
	{
		get { return (UIElement)GetValue(TargetElementProperty); }
		set { SetValue(TargetElementProperty, value); }
	}
	public static readonly DependencyProperty TargetElementProperty =
		DependencyProperty.Register(
			"TargetElement",
			typeof(UIElement),
			typeof(FocusElementAction),
			new PropertyMetadata(default(UIElement)));

	protected override void Invoke(object parameter)
	{
		if (TargetElement is { Focusable: true, IsFocused: false })
			TargetElement.Focus();
	}
}