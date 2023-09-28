using System.Windows;
using System.Windows.Media;

namespace Monitorian.Core.Views.Controls;

public static class FrameworkElementSize
{
	public static double GetConsistentHeight(DependencyObject obj)
	{
		return (double)obj.GetValue(ConsistentHeightProperty);
	}
	public static void SetConsistentHeight(DependencyObject obj, double value)
	{
		obj.SetValue(ConsistentHeightProperty, value);
	}
	public static readonly DependencyProperty ConsistentHeightProperty =
		DependencyProperty.RegisterAttached(
			"ConsistentHeight",
			typeof(double),
			typeof(FrameworkElementSize),
			new PropertyMetadata(
				0D,
				(d, e) =>
				{
					if (d is not FrameworkElement element)
						return;

					var value = (double)e.NewValue;

					var dpi = VisualTreeHelper.GetDpi(element);
					if (dpi.DpiScaleY > 0)
						element.Height = value / dpi.DpiScaleY;

					//var window = Window.GetWindow(element);
					//if (window is not null)
					//{
					//	window.DpiChanged += (_, args) =>
					//	{
					//		var dpi = args.NewDpi;
					//		if (dpi.DpiScaleY > 0)
					//			element.Height = value / dpi.DpiScaleY;
					//	};
					//}
				}));
}