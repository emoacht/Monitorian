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
		/// Gets the color theme for Windows.
		/// </summary>
		/// <returns>Color theme</returns>
		public static ColorTheme GetWindowsTheme() => GetTheme(SystemValueName);

		/// <summary>
		/// Gets the color theme for applications.
		/// </summary>
		/// <returns>Color theme</returns>
		public static ColorTheme GetAppTheme() => GetTheme(AppValueName);

		private const string SystemValueName = "SystemUsesLightTheme";
		private const string AppValueName = "AppsUseLightTheme";

		private static ColorTheme GetTheme(string valueName)
		{
			const string keyName = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"; // HKCU

			using var key = Registry.CurrentUser.OpenSubKey(keyName);

			return key?.GetValue(valueName, 1) switch
			{
				0 => ColorTheme.Dark,
				1 => ColorTheme.Light,
				_ => ColorTheme.Unknown
			};
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

	/// <summary>
	/// Color theme
	/// </summary>
	public enum ColorTheme
	{
		/// <summary>
		/// Unknown
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Dark mode
		/// </summary>
		Dark,

		/// <summary>
		/// Light mode
		/// </summary>
		Light
	}
}