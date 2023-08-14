using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Monitorian.Core.Views
{
	/// <summary>
	/// A wrapper class of <see cref="Windows.UI.ViewManagement.UISettings"/>
	/// </summary>
	/// <remarks>
	/// <see cref="Windows.UI.ViewManagement.UISettings"/> is only available
	/// on Windows 10 (version 10.0.10240.0) or greater.
	/// </remarks>
	public static class UISettings
	{
		private static Windows.UI.ViewManagement.UISettings _uiSettings;

		/// <summary>
		/// Gets the system accent color.
		/// </summary>
		/// <returns></returns>
		public static Color GetAccentColor() => GetUIColor(Windows.UI.ViewManagement.UIColorType.Accent);

		/// <summary>
		/// Gets the system accent light color.
		/// </summary>
		/// <returns></returns>
		public static Color GetAccentLightColor() => GetUIColor(Windows.UI.ViewManagement.UIColorType.AccentLight1);

		/// <summary>
		/// Gets the system accent dark color.
		/// </summary>
		/// <returns></returns>
		public static Color GetAccentDarkColor() => GetUIColor(Windows.UI.ViewManagement.UIColorType.AccentDark1);

		/// <summary>
		/// Gets the system background color.
		/// </summary>
		/// <returns></returns>
		public static Color GetBackgroundColor() => GetUIColor(Windows.UI.ViewManagement.UIColorType.Background);

		private static Color GetUIColor(Windows.UI.ViewManagement.UIColorType colorType)
		{
			var value = (_uiSettings ?? new Windows.UI.ViewManagement.UISettings()).GetColorValue(colorType);
			return Color.FromArgb(value.A, value.R, value.G, value.B);
		}

		private static readonly object _lock = new();

		/// <summary>
		/// Occurs when colors have changed.
		/// </summary>
		/// <remarks>
		/// UISettings.ColorValuesChanged event seems not to fire when this assembly is packaged.
		/// </remarks>
		public static event EventHandler ColorsChanged
		{
			add
			{
				lock (_lock)
				{
					_colorsChanged += value;

					if (_uiSettings is null)
					{
						_uiSettings = new Windows.UI.ViewManagement.UISettings();
						_uiSettings.ColorValuesChanged += OnColorValuesChanged;
					}
				}
			}
			remove
			{
				lock (_lock)
				{
					_colorsChanged -= value;
					if (_colorsChanged is not null)
						return;

					if (_uiSettings is not null)
					{
						_uiSettings.ColorValuesChanged -= OnColorValuesChanged;
						_uiSettings = null;
					}
				}
			}
		}
		private static event EventHandler _colorsChanged;

		private static void OnColorValuesChanged(Windows.UI.ViewManagement.UISettings sender, object args)
		{
			_colorsChanged?.Invoke(sender, EventArgs.Empty);
		}
	}
}