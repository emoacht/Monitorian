using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Core.Views.Converters;

[ValueConversion(typeof(Visibility), typeof(bool))]
public class VisibilityToBooleanFilterConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not Visibility sourceValue)
			return DependencyProperty.UnsetValue;

		var targetValue = (sourceValue == Visibility.Visible);

		if (IsFilteredOut(targetValue, parameter))
			return DependencyProperty.UnsetValue;

		return targetValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not bool targetValue)
			return DependencyProperty.UnsetValue;

		if (IsFilteredOut(targetValue, parameter))
			return DependencyProperty.UnsetValue;

		return targetValue ? Visibility.Visible : Visibility.Collapsed;
	}

	private static bool IsFilteredOut(bool targetValue, object parameter)
	{
		if ((parameter is bool expectedValue) || bool.TryParse(parameter as string, out expectedValue))
			return (targetValue != expectedValue);

		return false;
	}
}