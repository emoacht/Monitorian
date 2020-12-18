using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Monitorian.Core.Views.Converters
{
	[ValueConversion(typeof(bool), typeof(bool))]
	public class BooleanInverseConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not bool sourceValue)
				return DependencyProperty.UnsetValue;

			return !sourceValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Convert(value, targetType, parameter, culture);
		}
	}
}