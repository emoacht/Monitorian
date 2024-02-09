using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public class FocusElementAction : TriggerAction<UIElement>
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
		var target = TargetElement ?? AssociatedObject;
		if (target is { Focusable: true, IsFocused: false })
			target.Focus();
	}
}