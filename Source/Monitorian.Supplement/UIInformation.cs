using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Windows.UI.ViewManagement;

namespace Monitorian.Supplement
{
	/// <summary>
	/// A wrapper class of <see cref="Windows.UI.ViewManagement.UISettings"/>
	/// </summary>
	/// <remarks>
	/// <see cref="Windows.UI.ViewManagement.UISettings"/> is available
	/// on Windows 10 (version 10.0.10240.0) or newer.
	/// </remarks>
	public static class UIInformation
	{
		private static UISettings _uiSettings;

		/// <summary>
		/// Gets the system accent color.
		/// </summary>
		/// <returns></returns>
		public static Color GetAccentColor() => GetUIColor(UIColorType.Accent);

		/// <summary>
		/// Gets the system accent light color.
		/// </summary>
		/// <returns></returns>
		public static Color GetAccentLightColor() => GetUIColor(UIColorType.AccentLight1);

		/// <summary>
		/// Gets the system accent dark color.
		/// </summary>
		/// <returns></returns>
		public static Color GetAccentDarkColor() => GetUIColor(UIColorType.AccentDark1);

		/// <summary>
		/// Gets the system background color.
		/// </summary>
		/// <returns></returns>
		public static Color GetBackgroundColor() => GetUIColor(UIColorType.Background);

		private static Color GetUIColor(UIColorType colorType)
		{
			var value = (_uiSettings ?? new UISettings()).GetColorValue(colorType);
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
						_uiSettings = new UISettings();
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

		private static void OnColorValuesChanged(UISettings sender, object args)
		{
			_colorsChanged?.Invoke(sender, EventArgs.Empty);
		}
	}
}