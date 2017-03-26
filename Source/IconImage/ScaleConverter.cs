using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace IconImage
{
	[ValueConversion(typeof(double), typeof(double))]
	public class ScaleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is double sourceValue) || !double.TryParse(parameter?.ToString(), out double factor))
				return DependencyProperty.UnsetValue;

			return sourceValue / factor;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is double targetValue) || !double.TryParse(parameter?.ToString(), out double factor))
				return DependencyProperty.UnsetValue;

			return targetValue * factor;
		}
	}
}