using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Monitorian.Core.Helper;
using ScreenFrame.Painter;

namespace Monitorian.Core.Views
{
	public class WindowPainter : ScreenFrame.Painter.WindowPainter
	{
		public WindowPainter() : base(AppKeeper.DefinedArguments)
		{ }

		public static new IReadOnlyCollection<string> Options => ScreenFrame.Painter.WindowPainter.Options.Concat(ColorPairs.Keys).ToArray();

		/// <summary>
		/// Color changeable elements of window
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
				if (colorPairs.TryGetValue(arguments[i], out ColorElement key) &&
					TryParse(arguments[i + 1], out Brush value))
				{
					colors[key] = value;
					i++;
				}
				i++;
			}

			_colors = colors.Any() ? colors : null;
		}

		protected override void PaintBackground(Window window)
		{
			if (ChangeColors(window))
				return;

			base.PaintBackground(window);
		}

		private bool ChangeColors(Window window)
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
	}
}