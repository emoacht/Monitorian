using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Views.Controls
{
	public class TemplateElement
	{
		public static Visibility GetVisibility(DependencyObject obj)
		{
			return (Visibility)obj.GetValue(VisibilityProperty);
		}
		public static void SetVisibility(DependencyObject obj, Visibility value)
		{
			obj.SetValue(VisibilityProperty, value);
		}
		public static readonly DependencyProperty VisibilityProperty =
			DependencyProperty.RegisterAttached(
				"Visibility",
				typeof(Visibility),
				typeof(TemplateElement),
				new PropertyMetadata(Visibility.Collapsed));
	}
}