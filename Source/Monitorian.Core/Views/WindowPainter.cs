using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using Monitorian.Core.Helper;
using Monitorian.Supplement;
using static Monitorian.Core.Views.WindowEffect;

namespace Monitorian.Core.Views
{
	public class WindowPainter : IDisposable
	{
		public WindowPainter()
		{
			CheckArguments();
			ApplyInitialTheme();
		}

		public static IReadOnlyCollection<string> Options => new[] { ThemeOption, TextureOption, RoundOption }.Concat(ColorPairs.Keys).ToArray();

		private const string ThemeOption = "/theme";

		private ColorTheme _theme;
		private bool _isThemeAdaptive = true; // Default

		/// <summary>
		/// Background texture of window
		/// </summary>
		private enum Texture
		{
			None = 0,

			/// <summary>
			/// Thin blur texture
			/// </summary>
			Thin,

			/// <summary>
			/// Thick blur (Acrylic) texture
			/// </summary>
			Thick
		}

		private const string TextureOption = "/texture";

		private Texture _texture = Texture.Thick; // Default

		private const string RoundOption = "/round";

		private bool _isRounded;

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

		private void CheckArguments()
		{
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

			var arguments = AppKeeper.DefinedArguments;

			int i = 0;
			while (i < arguments.Count)
			{
				if (arguments[i] == RoundOption)
				{
					_isRounded = true;
				}
				else if (i < arguments.Count - 1)
				{
					if (arguments[i] == ThemeOption)
					{
						if (Enum.TryParse(arguments[i + 1], true, out ColorTheme buffer))
						{
							_theme = buffer;
							_isThemeAdaptive = false;
						}
					}
					else if (arguments[i] == TextureOption)
					{
						if (Enum.TryParse(arguments[i + 1], true, out Texture buffer))
							_texture = buffer;
					}
					else if (colorPairs.TryGetValue(arguments[i], out ColorElement key))
					{
						if (TryParse(arguments[i + 1], out Brush value))
						{
							colors[key] = value;
							i++;
						}
					}
				}
				i++;
			}

			_colors = colors.Any() ? colors : null;
		}

		#region Window

		private readonly List<Window> _windows = new();

		public void Add(Window window)
		{
			window.SourceInitialized += OnSourceInitialized;
			window.Closed += OnClosed;
			_windows.Add(window);
		}

		private void Remove(Window window)
		{
			window.SourceInitialized -= OnSourceInitialized;
			window.Closed -= OnClosed;
			_windows.Remove(window);
		}

		private void OnSourceInitialized(object sender, EventArgs e)
		{
			var window = (Window)sender;
			DisableTransitions(window);
			PaintBackground(window);

			if (_isThemeAdaptive)
				AddHook(window);
		}

		private void OnClosed(object sender, EventArgs e)
		{
			var oldWindow = (Window)sender;
			Remove(oldWindow);

			if (_isThemeAdaptive &&
				RemoveHook(oldWindow))
			{
				var newWindow = _windows.FirstOrDefault(x => x.IsInitialized);
				if (newWindow is not null)
					AddHook(newWindow);
			}
		}

		private Window _window;
		private HwndSource _source;

		private void AddHook(Window window)
		{
			if (_window is not null)
				return;

			_window = window;
			_source = PresentationSource.FromVisual(window) as HwndSource;
			_source?.AddHook(WndProc);
		}

		private bool RemoveHook(Window window)
		{
			if ((_window is not null) &&
				(_window == window))
			{
				RemoveHook();
				return true;
			}
			return false;
		}

		private void RemoveHook()
		{
			_window = null;
			_source?.RemoveHook(WndProc);
			_source = null;
		}

		private const int WM_SETTINGCHANGE = 0x001A;

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_SETTINGCHANGE:
					if (string.Equals(Marshal.PtrToStringAuto(lParam), "ImmersiveColorSet"))
					{
						OnChanged();
					}
					break;
			}
			return IntPtr.Zero;
		}

		private Task _throttleTask;

		private async void OnChanged()
		{
			var waitTask = Task.Delay(TimeSpan.FromSeconds(1));
			_throttleTask = waitTask;
			await waitTask;
			if (_throttleTask == waitTask)
			{
				if (ApplyChangedTheme())
				{
					ThemeChanged?.Invoke(null, EventArgs.Empty);
				}
			}
		}

		#endregion

		public event EventHandler ThemeChanged;

		private void ApplyInitialTheme()
		{
			switch (_theme)
			{
				case ColorTheme.Dark:
					// Leave as is.
					return;

				case ColorTheme.Light:
					break;

				default:
					_theme = UIInformation.GetWindowsTheme();
					break;
			}
			ApplyTheme(_theme);
		}

		private bool ApplyChangedTheme()
		{
			var theme = UIInformation.GetWindowsTheme();
			if (_theme == theme)
				return false;

			_theme = theme;
			ApplyTheme(_theme);

			ResetTranslucent();

			_windows.ForEach(x => x.Dispatcher.Invoke(() => PaintBackground(x)));
			return true;
		}

		private bool PaintBackground(Window window)
		{
			if (ChangeColors(window) || (_texture == Texture.None))
				return false;

			if (OsVersion.Is11OrGreater)
			{
				// For Windows 11
				if (_isRounded)
					SetCornersForWin11(window);
			}

			if (OsVersion.Is10OrGreater)
			{
				// For Windows 10 and 11
				if (!IsTransparencyEnabledForWin10)
					return false;

				if (!TryGetTranslucent(_texture, out Brush brush, out Color? color))
					return false;

				window.Background = brush;
				return EnableBackgroundBlurForWin10(window, color);
			}

			if (OsVersion.Is8OrGreater)
			{
				// For Windows 8 and 8.1, no blur effect is available.
				return false;
			}

			if (OsVersion.Is7OrGreater)
			{
				// For Windows 7
				if (!IsTransparencyEnabledForWin7)
					return false;

				if (!TryGetTranslucent(Texture.None, out Brush brush, out _))
					return false;

				window.Background = brush;
				return EnableBackgroundBlurForWin7(window);
			}

			return false;
		}

		private bool ChangeColors(Window window)
		{
			if (_colors?.Any() is not true)
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

		private const string TranslucentBrushKey = "App.Background.Translucent";
		private static SolidColorBrush _translucentBrush;
		private static Color? _translucentColor;

		private static bool TryGetTranslucent(Texture texture, out Brush brush, out Color? color)
		{
			if (_translucentBrush is null)
			{
				if (Application.Current.TryFindResource(TranslucentBrushKey) is SolidColorBrush buffer)
				{
					_translucentBrush = buffer;

					if (texture == Texture.Thick)
					{
						var value = _translucentBrush.Color;
						_translucentBrush = new SolidColorBrush(Color.FromArgb(a: 1, r: value.R, g: value.G, b: value.B));
						_translucentColor = Color.FromArgb(a: (byte)Math.Max(0, value.A - 1), r: value.R, g: value.G, b: value.B);
					}
				}
				else
				{
					brush = default;
					color = default;
					return false;
				}
			}
			brush = _translucentBrush;
			color = _translucentColor;
			return true;
		}

		private static void ResetTranslucent()
		{
			_translucentBrush = null;
		}

		#region Resource

		private static void ApplyTheme(ColorTheme theme)
		{
			//const string DarkThemeUriString = @"/Monitorian.Core;component/Views/Themes/DarkTheme.xaml";
			const string LightThemeUriString = @"/Monitorian.Core;component/Views/Themes/LightTheme.xaml";

			switch (theme)
			{
				case ColorTheme.Dark:
					ApplyResource(null, LightThemeUriString);
					break;

				case ColorTheme.Light:
					ApplyResource(LightThemeUriString, null);
					break;
			}
		}

		private static void ApplyResource(string newUriString, string oldUriString)
		{
			if (!string.IsNullOrWhiteSpace(oldUriString))
			{
				var oldDictionary = Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.OriginalString == oldUriString);
				if (oldDictionary is not null)
					Application.Current.Resources.MergedDictionaries.Remove(oldDictionary);
			}

			if (!string.IsNullOrWhiteSpace(newUriString))
			{
				try
				{
					var newDictionary = new ResourceDictionary { Source = new Uri(newUriString, UriKind.RelativeOrAbsolute) };
					Application.Current.Resources.MergedDictionaries.Add(newDictionary);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Failed to apply resources." + Environment.NewLine
						+ ex);
				}
			}
		}

		#endregion

		#region IDisposable

		private bool _isDisposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				RemoveHook();
				ThemeChanged = null;
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}