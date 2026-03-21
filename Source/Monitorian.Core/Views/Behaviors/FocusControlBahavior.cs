using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors;

public class FocusControlBahavior : Behavior<ContentControl>
{
	protected override void OnAttached()
	{
		base.OnAttached();

		//this.AssociatedObject.MouseEnter += OnMouseEnter;
		this.AssociatedObject.MouseLeave += OnMouseLeave;
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();

		//this.AssociatedObject.MouseEnter -= OnMouseEnter;
		this.AssociatedObject.MouseLeave -= OnMouseLeave;
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		var scope = FocusManager.GetFocusScope(this.AssociatedObject);
		if (this.AssociatedObject.Content is IInputElement element)
			FocusManager.SetFocusedElement(scope, element);
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		var scope = FocusManager.GetFocusScope(this.AssociatedObject);
		FocusManager.SetFocusedElement(scope, null);

		if (Window.GetWindow(this.AssociatedObject) is Window window)
			window.Focus();
	}
}