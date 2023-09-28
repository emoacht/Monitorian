using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Core.Views.Converters;

[ValueConversion(typeof(Visibility), typeof(Visibility))]
public class VisibilityInverseConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not Visibility sourceValue)
			return DependencyProperty.UnsetValue;

		return (sourceValue != Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Convert(value, targetType, parameter, culture);
	}
}