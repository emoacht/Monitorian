using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Monitorian.Core.Helper;
using ScreenFrame.Painter;

namespace Monitorian.Core.Views;

public class WindowPainter : ScreenFrame.Painter.WindowPainter
{
	public WindowPainter() : base(AppKeeper.StandardArguments)
	{ }

	public static new IReadOnlyCollection<string> Options => ScreenFrame.Painter.WindowPainter.Options.Concat(ColorPairs.Keys).ToArray();

	/// <summary>
	/// Color changeable background/border
	/// </summary>
	private enum ColorElement
	{
		None = 0,
		MainBorder,
		MainBackground,
		MenuBorder,
		MenuBackground
	}

	private static IReadOnlyDictionary<string, ColorElement> ColorPairs => new Dictionary<string, ColorElement>
	{
		{ "/main_border", ColorElement.MainBorder },
		{ "/main_background", ColorElement.MainBackground },
		{ "/menu_border", ColorElement.MenuBorder },
		{ "/menu_background", ColorElement.MenuBackground }
	};

	private Dictionary<ColorElement, Brush> _colors;

	protected override void CheckArguments(IReadOnlyList<string> arguments)
	{
		base.CheckArguments(arguments);

		var converter = new BrushConverter();
		bool TryParse(string source, out Brush brush)
		{
			try
			{
				brush = (Brush)converter.ConvertFromString(source);
				return true;
			}
			catch
			{
				brush = null;
				return false;
			}
		}

		var colorPairs = ColorPairs;
		var colors = new Dictionary<ColorElement, Brush>();

		int i = 0;
		while (i < arguments.Count - 1)
		{
			if (colorPairs.TryGetValue(arguments[i], out ColorElement key)
				&& TryParse(arguments[i + 1], out Brush value))
			{
				colors[key] = value;
				i++;
			}
			i++;
		}

		_colors = colors.Any() ? colors : null;
	}

	#region Theme or background/border colors

	protected override void PaintBackground(Window window)
	{
		if (ChangeBackgroundColors(window))
			return;

		base.PaintBackground(window);
	}

	private bool ChangeBackgroundColors(Window window)
	{
		if (_colors is not { Count: > 0 })
			return false;

		var isBackgroundChanged = false;

		foreach (var (key, value) in _colors)
		{
			switch (key, window is MainWindow)
			{
				case (ColorElement.MainBorder, true):
				case (ColorElement.MenuBorder, false):
					window.BorderBrush = value;
					window.BorderThickness = new Thickness(1);
					break;

				case (ColorElement.MainBackground, true):
				case (ColorElement.MenuBackground, false):
					window.Background = value;
					isBackgroundChanged = true;
					break;
			}
		}
		return isBackgroundChanged;
	}

	protected override string TranslucentBrushKey { get; } = "App.Background.Translucent";

	protected override void ChangeThemes(ColorTheme oldTheme, ColorTheme newTheme)
	{
		//const string DarkThemeUriString = @"/Monitorian.Core;component/Views/Themes/DarkTheme.xaml";
		const string LightThemeUriString = @"/Monitorian.Core;component/Views/Themes/LightTheme.xaml";

		switch (oldTheme, newTheme)
		{
			case (ColorTheme.Unknown, ColorTheme.Dark):
				// Leave as is.
				break;

			case (ColorTheme.Light, ColorTheme.Dark):
				ChangeResources(oldUriString: LightThemeUriString, newUriString: null);
				break;

			case (ColorTheme.Unknown, ColorTheme.Light):
			case (ColorTheme.Dark, ColorTheme.Light):
				ChangeResources(oldUriString: null, newUriString: LightThemeUriString);
				break;
		}
	}

	public string GetIconPath()
	{
		return Theme switch
		{
			ColorTheme.Light => "pack://application:,,,/Monitorian.Core;component/Resources/Icons/LightTrayIcon.ico",
			ColorTheme.Dark or _ => "pack://application:,,,/Monitorian.Core;component/Resources/Icons/DarkTrayIcon.ico",
		};
	}

	#endregion

	#region Accent color

	/// <summary>
	/// Whether the accent color is supported
	/// </summary>
	/// <remarks>
	/// The accent color on Windows 8.1 seems not to have shaded variants and so is not considered
	/// as utilizable here.
	/// </remarks>
	public bool IsAccentColorSupported { get; } = OsVersion.Is10OrGreater;

	private class ColorContainer
	{
		private static readonly ResourceDictionary _generic;

		static ColorContainer() => _generic = Application.Current.Resources.MergedDictionaries.Single(x => x.Source.OriginalString.EndsWith("Generic.xaml"));

		private readonly string _key;
		private readonly Color _originalColor;

		public ColorContainer(string key)
		{
			this._key = key;
			_originalColor = Color;
		}

		public Color Color
		{
			get => (Color)_generic[_key];
			set => _generic[_key] = value;
		}

		public void Revert() => Color = _originalColor;
	}

	private readonly Lazy<ColorContainer> _staticColorContainer = new(() => new("App.Background.Accent.StaticColor"));
	private readonly Lazy<ColorContainer> _mouseOverColorContainer = new(() => new("App.Background.Accent.MouseOverColor"));
	private readonly Lazy<ColorContainer> _pressedColorContainer = new(() => new("App.Background.Accent.PressedColor"));

	public void AttachAccentColors()
	{
		if (!IsAccentColorSupported)
			return;

		ChangeAccentColors();

		RespondsAccentColorChanged = true;
	}

	public void DetachAccentColors()
	{
		RespondsAccentColorChanged = false;

		_staticColorContainer.Value.Revert();
		_mouseOverColorContainer.Value.Revert();
		_pressedColorContainer.Value.Revert();
	}

	protected override void ChangeAccentColors()
	{
		_staticColorContainer.Value.Color = UISettings.GetAccentColor();
		_mouseOverColorContainer.Value.Color = UISettings.GetAccentLightColor();
		_pressedColorContainer.Value.Color = _mouseOverColorContainer.Value.Color;
	}

	#endregion
}