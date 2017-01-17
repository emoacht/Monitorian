using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Views.Converters
{
	[ValueConversion(typeof(Visibility), typeof(bool))]
	public class VisibilityToBooleanFilterConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is Visibility))
				return DependencyProperty.UnsetValue;

			var targetValue = ((Visibility)value == Visibility.Visible) ? true : false;

			if (IsFilteredOut(targetValue, parameter))
				return DependencyProperty.UnsetValue;

			return targetValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool))
				return DependencyProperty.UnsetValue;

			var targetValue = (bool)value;

			if (IsFilteredOut(targetValue, parameter))
				return DependencyProperty.UnsetValue;

			return targetValue ? Visibility.Visible : Visibility.Collapsed;
		}

		private static bool IsFilteredOut(bool targetValue, object parameter)
		{
			bool expectedValue;
			if (!bool.TryParse((parameter as string ?? parameter?.ToString()), out expectedValue))
				return false;

			return (expectedValue != targetValue);
		}
	}
}