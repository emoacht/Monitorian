﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Core.Views.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BooleanToVisibilityTurnConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not bool sourceValue)
			return DependencyProperty.UnsetValue;

		return (sourceValue ^ IsTurn(parameter)) ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not Visibility sourceValue)
			return DependencyProperty.UnsetValue;

		return (sourceValue is Visibility.Visible) ^ IsTurn(parameter);
	}

	private static bool IsTurn(object parameter)
	{
		if ((parameter is bool turnValue) || bool.TryParse(parameter as string, out turnValue))
			return turnValue;

		return false;
	}
}