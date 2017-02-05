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
			double factor;
			if (!(value is double) || !double.TryParse(parameter.ToString(), out factor))
				return DependencyProperty.UnsetValue;

			return (double)value / factor;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double factor;
			if (!(value is double) || !double.TryParse(parameter.ToString(), out factor))
				return DependencyProperty.UnsetValue;

			return (double)value * factor;
		}
	}
}