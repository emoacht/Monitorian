using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace StartupBridge.Storage
{
	/// <summary>
	/// Accessor to <see cref="Windows.Storage.ApplicationData.LocalSettings"/> or <see cref="Windows.Storage.ApplicationData.RoamingSettings"/>
	/// </summary>
	/// <remarks>
	/// Usable data types are shown at:
	/// https://docs.microsoft.com/en-us/windows/uwp/design/app-settings/store-and-retrieve-app-data#types-of-app-data
	/// plus byte[] and Enum.
	/// </remarks>
	internal class SettingsAccessor
	{
		public static SettingsAccessor Local => _local.Value;
		private static readonly Lazy<SettingsAccessor> _local = new Lazy<SettingsAccessor>(() =>
			new SettingsAccessor(ApplicationData.Current.LocalSettings));

		public static SettingsAccessor Roaming => _roaming.Value;
		private static readonly Lazy<SettingsAccessor> _roaming = new Lazy<SettingsAccessor>(() =>
			new SettingsAccessor(ApplicationData.Current.RoamingSettings));

		private readonly IPropertySet _values;

		private SettingsAccessor(ApplicationDataContainer settings)
		{
			this._values = settings.Values;
		}

		private readonly object _lock = new object();

		#region Get

		public T GetValue<T>([CallerMemberName] string propertyName = null)
		{
			return TryGetValue(out T propertyValue, propertyName)
				? propertyValue
				: default;
		}

		public T GetValue<T>(T defaultValue, [CallerMemberName] string propertyName = null)
		{
			return TryGetValue(out T propertyValue, propertyName)
				? propertyValue
				: defaultValue;
		}

		public bool TryGetValue<T>(out T propertyValue, [CallerMemberName] string propertyName = null)
		{
			lock (_lock)
			{
				if (_values.TryGetValue(propertyName, out object value))
				{
					propertyValue = (T)value;
					return true;
				}
				propertyValue = default;
				return false;
			}
		}

		public bool TryGetCompositeItemValue<T>(out T itemValue, string itemKey, string compositeName)
		{
			lock (_lock)
			{
				if (_values.TryGetValue(compositeName, out object compositeValue)
					&& (compositeValue is ApplicationDataCompositeValue composite)
					&& composite.TryGetValue(itemKey, out object value))
				{
					itemValue = (T)value;
					return true;
				}
				itemValue = default;
				return false;
			}
		}

		#endregion

		#region Set

		public void SetValue<T>(T propertyValue, [CallerMemberName] string propertyName = null)
		{
			lock (_lock)
			{
				var value = ConvertIfEnum(propertyValue);

				// Add or change value.
				_values[propertyName] = value;
			}
		}

		public void SetCompositeItemValue<T>(T itemValue, string itemKey, string compositeName)
		{
			lock (_lock)
			{
				var value = ConvertIfEnum(itemValue);

				if (_values.TryGetValue(compositeName, out object compositeValue)
					&& (compositeValue is ApplicationDataCompositeValue composite))
				{
					// Add or change item value and then update composite value.
					composite[itemKey] = value;
					_values[compositeName] = composite;
				}
				else
				{
					// Add or overwrite composite value.
					_values[compositeName] = new ApplicationDataCompositeValue { [itemKey] = value };
				}
			}
		}

		private static object ConvertIfEnum<T>(T source)
		{
			return typeof(T).IsEnum
				? Convert.ChangeType(source, Enum.GetUnderlyingType(typeof(T)))
				: source;
		}

		#endregion

		#region Remove

		public bool RemoveValue([CallerMemberName] string propertyName = null)
		{
			lock (_lock)
			{
				return _values.Remove(propertyName);
			}
		}

		public bool RemoveCompositeItemValue(string itemKey, string compositeName)
		{
			lock (_lock)
			{
				if (_values.TryGetValue(compositeName, out object compositeValue)
					&& (compositeValue is ApplicationDataCompositeValue composite))
				{
					// Remove item and then update composite value.
					if (composite.Remove(itemKey))
					{
						if (composite.Any())
						{
							// If composite value still has any item, update composite value.
							_values[compositeName] = composite;
							return true;
						}
						else
						{
							// Otherwise, remove composite value.
							return _values.Remove(compositeName);
						}
					}
				}
				return false;
			}
		}

		#endregion
	}
}