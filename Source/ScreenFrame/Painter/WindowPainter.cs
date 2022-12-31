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

using ScreenFrame.Helper;
using static ScreenFrame.Painter.WindowEffect;

namespace ScreenFrame.Painter
{
	/// <summary>
	/// Painter of <see cref="System.Windows.Window"/>
	/// </summary>
	public abstract class WindowPainter : IDisposable
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="args">Arguments</param>
		public WindowPainter(IReadOnlyList<string> args)
		{
			CheckArguments(args);
			ApplyInitialTheme();
		}

		/// <summary>
		/// Options
		/// </summary>
		protected static IReadOnlyCollection<string> Options => new[] { ThemeOption, TextureOption, CornerOption };

		private const string ThemeOption = "/theme";

		/// <summary>
		/// Current theme
		/// </summary>
		protected ColorTheme Theme { get; private set; }

		/// <summary>
		/// Background texture of window
		/// </summary>
		private enum BackgroundTexture
		{
			/// <summary>
			/// None
			/// </summary>
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

		private BackgroundTexture _texture = BackgroundTexture.Thick; // Default

		private const string CornerOption = "/corner";

		private CornerPreference _corner;

		/// <summary>
		/// Checks command-line arguments
		/// </summary>
		/// <param name="arguments">Arguments</param>
		protected virtual void CheckArguments(IReadOnlyList<string> arguments)
		{
			if (arguments is null)
				return;

			int i = 0;
			while (i < arguments.Count - 1)
			{
				switch (arguments[i])
				{
					case ThemeOption when Enum.TryParse(arguments[i + 1], true, out ColorTheme buffer):
						Theme = buffer;
						RespondsThemeChanged = false;
						i++;
						break;

					case TextureOption when Enum.TryParse(arguments[i + 1], true, out BackgroundTexture buffer):
						_texture = buffer;
						i++;
						break;

					case CornerOption when Enum.TryParse(arguments[i + 1], true, out CornerPreference buffer):
						_corner = buffer;
						i++;
						break;
				}
				i++;
			}
		}

		#region Window

		private readonly List<Window> _windows = new();

		/// <summary>
		/// Adds a window to be painted.
		/// </summary>
		/// <param name="window">Window to be painted</param>
		public void Add(Window window)
		{
			window.SourceInitialized += OnSourceInitialized;
			window.Closed += OnClosed;
			_windows.Add(window);
		}

		/// <summary>
		/// Removes a window to be painted.
		/// </summary>
		/// <param name="window">Window to be painted</param>
		protected void Remove(Window window)
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

			AddHook(window);
		}

		private void OnClosed(object sender, EventArgs e)
		{
			var oldWindow = (Window)sender;
			Remove(oldWindow);

			if (RemoveHook(oldWindow))
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
		private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_SETTINGCHANGE when (Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet") && RespondsThemeChanged:
					OnThemeChanged();
					break;

				case WM_DWMCOLORIZATIONCOLORCHANGED when RespondsAccentColorChanged:
					OnAccentColorChanged(ColorExtension.FromUInt32((uint)wParam));
					break;
			}
			return IntPtr.Zero;
		}

		private Throttle _applyChangedTheme;
		private Throttle<Color> _applyChangedAccentColor;

		private async void OnThemeChanged()
		{
			_applyChangedTheme ??= new Throttle(() =>
			{
				if (ApplyChangedTheme())
				{
					ThemeChanged?.Invoke(null, EventArgs.Empty);
				}
			});
			await _applyChangedTheme.PushAsync();
		}

		private async void OnAccentColorChanged(Color color)
		{
			_applyChangedAccentColor ??= new Throttle<Color>(c =>
			{
				if (ApplyChangedAccentColor(c))
				{
					AccentColorChanged?.Invoke(null, EventArgs.Empty);
				}
			});
			await _applyChangedAccentColor.PushAsync(color);
		}

		#endregion

		#region Theme

		/// <summary>
		/// Whether to respond when the color theme for Windows is changed
		/// </summary>
		protected bool RespondsThemeChanged { get; set; } = true; // Default

		/// <summary>
		/// Occurs when the color theme for Windows is changed.
		/// </summary>
		public event EventHandler ThemeChanged;

		private void ApplyInitialTheme()
		{
			if (RespondsThemeChanged)
				Theme = ThemeInfo.GetWindowsTheme();

			ChangeThemes(oldTheme: ColorTheme.Unknown, newTheme: Theme);
		}

		private bool ApplyChangedTheme()
		{
			var oldTheme = Theme;
			Theme = ThemeInfo.GetWindowsTheme();
			if (Theme == oldTheme)
				return false;

			ChangeThemes(oldTheme: oldTheme, newTheme: Theme);

			ResetTranslucent();

			_windows.ForEach(x => x.Dispatcher.Invoke(() => PaintBackground(x)));
			return true;
		}

		/// <summary>
		/// Paints background of window.
		/// </summary>
		/// <param name="window">Window to be painted</param>
		protected virtual void PaintBackground(Window window)
		{
			if (_texture == BackgroundTexture.None)
				return;

			if (OsVersion.Is11OrGreater)
			{
				// For Windows 11
				if (_corner != CornerPreference.NotRound)
					SetCornersForWin11(window, CornerPreference.Round);
			}

			if (OsVersion.Is10OrGreater)
			{
				// For Windows 10 and 11
				if (!IsTransparencyEnabledForWin10)
					return;

				if (!TryGetTranslucent(_texture, out Brush brush, out Color? color))
					return;

				window.Background = brush;
				EnableBackgroundBlurForWin10(window, color);
			}

			if (OsVersion.Is8OrGreater)
			{
				// For Windows 8 and 8.1, no blur effect is available.
				return;
			}

			if (OsVersion.Is7OrGreater)
			{
				// For Windows 7
				if (!IsTransparencyEnabledForWin7)
					return;

				if (!TryGetTranslucent(BackgroundTexture.None, out Brush brush, out _))
					return;

				window.Background = brush;
				EnableBackgroundBlurForWin7(window);
			}
		}

		/// <summary>
		/// Background translucent SolidColorBrush key
		/// </summary>
		protected abstract string TranslucentBrushKey { get; }

		private SolidColorBrush _translucentBrush;
		private Color? _translucentColor;

		private bool TryGetTranslucent(BackgroundTexture texture, out Brush brush, out Color? color)
		{
			if (_translucentBrush is null)
			{
				if (!string.IsNullOrEmpty(TranslucentBrushKey) &&
					Application.Current.TryFindResource(TranslucentBrushKey) is SolidColorBrush buffer)
				{
					_translucentBrush = buffer;

					if (texture == BackgroundTexture.Thick)
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

		private void ResetTranslucent()
		{
			_translucentBrush = null;
		}

		/// <summary>
		/// Changes color themes.
		/// </summary>
		/// <param name="oldTheme">Old color theme</param>
		/// <param name="newTheme">New color theme</param>
		protected abstract void ChangeThemes(ColorTheme oldTheme, ColorTheme newTheme);

		/// <summary>
		/// Changes resources.
		/// </summary>
		/// <param name="oldUriString">Old resources' URI string</param>
		/// <param name="newUriString">New resources' URI string</param>
		protected static void ChangeResources(string oldUriString, string newUriString)
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
					Debug.WriteLine($"Failed to add resources." + Environment.NewLine
						+ ex);
				}
			}
		}

		#endregion

		#region Accent color

		/// <summary>
		/// Whether to respond when the accent color for Windows is changed
		/// </summary>
		protected bool RespondsAccentColorChanged
		{
			get => _respondsAccentColorChanged;
			set
			{
				_respondsAccentColorChanged = value;
				if (_respondsAccentColorChanged)
					AccentColor = ColorExtension.GetColorizationColor();
			}
		}
		private bool _respondsAccentColorChanged;

		/// <summary>
		/// Occurs when the accent color for Windows is changed.
		/// </summary>
		public event EventHandler AccentColorChanged;

		/// <summary>
		/// The accent color for Windows
		/// </summary>
		/// <remarks>
		/// This color is obtained by DwmGetColorizationColor function and then replaced with the color
		/// provided along with WM_DWMCOLORIZATIONCOLORCHANGED message.
		/// This color should not be used as is on Windows 10 because it will not match the actual color
		/// obtainable by Windows.UI.ViewManagement.UISettings.
		/// </remarks>
		protected static Color AccentColor { get; private set; }

		private bool ApplyChangedAccentColor(Color accentColor)
		{
			if (AccentColor == accentColor)
				return false;

			AccentColor = accentColor;
			ChangeAccentColors();
			return true;
		}

		/// <summary>
		/// Changes accent colors.
		/// </summary>
		protected virtual void ChangeAccentColors()
		{ }

		#endregion

		#region IDisposable

		private bool _isDisposed = false;

		/// <summary>
		/// Public implementation of Dispose pattern
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Protected implementation of Dispose pattern
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				RemoveHook();
				ThemeChanged = null;
				AccentColorChanged = null;
			}

			_isDisposed = true;
		}

		#endregion
	}
}