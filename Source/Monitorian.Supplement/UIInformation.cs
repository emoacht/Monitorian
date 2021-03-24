using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Win32;
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
	public class UIInformation
	{
		private static UISettings _uiSettings;

		/// <summary>
		/// Gets the system accent color.
		/// </summary>
		/// <returns></returns>
		public static Color GetAccentColor() => GetUIColor(UIColorType.Accent);

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

		/// <summary>
		/// Determines whether light theme is used by the system.
		/// </summary>
		/// <returns>True if light theme is used</returns>
		public static bool IsLightThemeUsed()
		{
			const string keyName = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"; // HKCU
			const string valueName = "SystemUsesLightTheme";

			using var key = Registry.CurrentUser.OpenSubKey(keyName);

			return (key?.GetValue(valueName) is 1);
		}

		private static readonly object _lock = new object();

		/// <summary>
		/// Occurs when colors have changed.
		/// </summary>
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