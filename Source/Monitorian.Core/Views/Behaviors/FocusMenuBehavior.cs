using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Monitorian.Core.Views.Behaviors
{
	public class FocusMenuBehavior : Behavior<UIElement>
	{
		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.MouseLeave += OnMouseLeave;
			this.AssociatedObject.IsKeyboardFocusedChanged += OnIsKeyboardFocusedChanged;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.MouseLeave -= OnMouseLeave;
			this.AssociatedObject.IsKeyboardFocusedChanged -= OnIsKeyboardFocusedChanged;
			this.AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			var scope = FocusManager.GetFocusScope(this.AssociatedObject);
			var element = FocusManager.GetFocusedElement(scope);

			// On Windows 11, enabling IME on TextBox causes MouseLeave event and changing the focus after
			// such event will hinder the input by IME. Thus, the case of TextBox shall be excluded.
			if (element is TextBoxBase)
				return;

			FocusManager.SetFocusedElement(scope, this.AssociatedObject); // UIElement.Focus method is not enough.
		}

		private void OnIsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				this.AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
			}
			else
			{
				this.AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
			}
		}

		private void OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Up:
				case Key.Down:
				case Key.Tab:
					e.Handled = true;
					this.AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
					break;
			}
		}
	}
}