using System.Windows.Media;

namespace Monitorian.Core.Views;

/// <summary>
/// A wrapper class of <see cref="Windows.UI.ViewManagement.UISettings"/>
/// </summary>
/// <remarks>
/// <see cref="Windows.UI.ViewManagement.UISettings"/> is only available
/// on Windows 10 (version 10.0.10240.0) or greater.
/// </remarks>
public static class UISettings
{
	// UISettings.ColorValuesChanged is not supported in desktop apps.
	// https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-supported-api#unsupported-members

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

	private static Windows.UI.ViewManagement.UISettings _uiSettings;

	private static Color GetUIColor(Windows.UI.ViewManagement.UIColorType colorType)
	{
		var value = (_uiSettings ??= new Windows.UI.ViewManagement.UISettings()).GetColorValue(colorType);
		return Color.FromArgb(value.A, value.R, value.G, value.B);
	}
}