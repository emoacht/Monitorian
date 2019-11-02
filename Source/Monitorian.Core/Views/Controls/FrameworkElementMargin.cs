using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Views.Controls
{
	public class FrameworkElementMargin
	{
		public static double GetHorizontal(DependencyObject obj)
		{
			return (double)obj.GetValue(HorizontalProperty);
		}
		public static void SetHorizontal(DependencyObject obj, double value)
		{
			obj.SetValue(HorizontalProperty, value);
		}
		public static readonly DependencyProperty HorizontalProperty =
			DependencyProperty.RegisterAttached(
				"Horizontal",
				typeof(double),
				typeof(FrameworkElementMargin),
				new PropertyMetadata(
					0D,
					(d, e) => SetMargin(d as FrameworkElement, (double)e.NewValue, 0D)));

		public static double GetVertical(DependencyObject obj)
		{
			return (double)obj.GetValue(VerticalProperty);
		}
		public static void SetVertical(DependencyObject obj, double value)
		{
			obj.SetValue(VerticalProperty, value);
		}
		public static readonly DependencyProperty VerticalProperty =
			DependencyProperty.RegisterAttached(
				"Vertical",
				typeof(double),
				typeof(FrameworkElementMargin),
				new PropertyMetadata(
					0D,
					(d, e) => SetMargin(d as FrameworkElement, 0D, (double)e.NewValue)));

		private static void SetMargin(FrameworkElement element, double horizontalMargin, double verticalMargin)
		{
			if (horizontalMargin < 0D)
				throw new ArgumentOutOfRangeException(nameof(horizontalMargin), horizontalMargin, "The margin must not be negative.");
			if (verticalMargin < 0D)
				throw new ArgumentOutOfRangeException(nameof(verticalMargin), verticalMargin, "The margin must not be negative.");

			var margin = element.Margin;
			if (0D < horizontalMargin)
				margin.Left = margin.Right = horizontalMargin;
			if (0D < verticalMargin)
				margin.Top = margin.Bottom = verticalMargin;

			element.Margin = margin;
		}
	}
}