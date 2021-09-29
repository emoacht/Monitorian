using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ScreenFrame.Painter
{
	/// <summary>
	/// Windows themes information
	/// </summary>
	public static class ThemeInfo
	{
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

			return key?.GetValue(valueName) switch
			{
				0 => ColorTheme.Dark,
				1 => ColorTheme.Light,
				_ => ColorTheme.Unknown
			};
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