using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using Monitorian.Core.Views.Input;

namespace Monitorian.Core.Views.Behaviors;

public class MouseHorizontalWheelBehavior : Behavior<UIElement>
{
	private static readonly MethodInfo _onMouseWheel;

	static MouseHorizontalWheelBehavior()
	{
		// Get UIElement.OnMouseWheel method information.
		_onMouseWheel = typeof(UIElement).GetMethod("OnMouseWheel", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, new Type[] { typeof(MouseWheelEventArgs) }, null);
	}

	protected override void OnAttached()
	{
		base.OnAttached();

		MouseAddition.AddMouseHorizontalWheelHandler(this.AssociatedObject, OnMouseHorizontalWheel);
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();

		MouseAddition.RemoveMouseHorizontalWheelHandler(this.AssociatedObject, OnMouseHorizontalWheel);
	}

	private void OnMouseHorizontalWheel(object sender, MouseWheelEventArgs e)
	{
		_onMouseWheel?.Invoke(this.AssociatedObject, new object[] { e });
	}
}