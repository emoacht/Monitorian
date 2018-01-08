using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace StartupBridge.Helper
{
	/// <summary>
	/// Accessor to LocalSettings
	/// </summary>
	/// <remarks>
	/// Usable value types are almost identical to types shown in:	
	/// https://msdn.microsoft.com/en-us/library/br205768.aspx
	/// excluding Uri and void but including byte[] and Enum.
	/// </remarks>
	internal class LocalSettingsAccessor
	{
		#region Getter

		public static T GetValue<T>([CallerMemberName] string propertyName = null)
		{
			return TryGetValue(out T propertyValue, propertyName)
				? propertyValue
				: default(T);
		}

		public static T GetValue<T>(T initialValue, [CallerMemberName] string propertyName = null)
		{
			return TryGetValue(out T propertyValue, propertyName)
				? propertyValue
				: initialValue;
		}

		public static bool TryGetValue<T>(out T propertyValue, [CallerMemberName] string propertyName = null)
		{
			var values = ApplicationData.Current.LocalSettings.Values;
			if (values.TryGetValue(propertyName, out object value))
			{
				propertyValue = (T)value;
				return true;
			}
			propertyValue = default(T);
			return false;
		}

		#endregion

		#region Setter

		public static void SetValue<T>(T propertyValue, [CallerMemberName] string propertyName = null)
		{
			var value = typeof(T).IsEnum
				? Convert.ChangeType(propertyValue, Enum.GetUnderlyingType(typeof(T)))
				: propertyValue;

			var values = ApplicationData.Current.LocalSettings.Values;
			if (!values.ContainsKey(propertyName))
			{
				// Add
				values.Add(propertyName, value);
			}
			else
			{
				// Change
				values[propertyName] = value;
			}
		}

		#endregion
	}
}