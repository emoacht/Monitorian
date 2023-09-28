using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Core.Views.Converters;

[ValueConversion(typeof(object), typeof(Visibility))]
public class ObjectToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return ((value is null) || string.IsNullOrWhiteSpace(value as string ?? value.ToString()))
			? Visibility.Collapsed
			: Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}