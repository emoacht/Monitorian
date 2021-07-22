﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

using Monitorian.Core.Helper;
using Monitorian.Supplement;

namespace Monitorian.Core.Views
{
	public static class WindowEffect
	{
		#region Win32 (common)

		[DllImport("Dwmapi.dll", SetLastError = true)]
		private static extern int DwmSetWindowAttribute(
			IntPtr hwnd,
			uint dwAttribute,
			[In] ref bool pvAttribute, // IntPtr
			uint cbAttribute);

		private enum DWMWA : uint
		{
			DWMWA_NCRENDERING_ENABLED = 1,     // [get] Is non-client rendering enabled/disabled
			DWMWA_NCRENDERING_POLICY,          // [set] Non-client rendering policy
			DWMWA_TRANSITIONS_FORCEDISABLED,   // [set] Potentially enable/forcibly disable transitions
			DWMWA_ALLOW_NCPAINT,               // [set] Allow contents rendered in the non-client area to be visible on the DWM-drawn frame.
			DWMWA_CAPTION_BUTTON_BOUNDS,       // [get] Bounds of the caption button area in window-relative space.
			DWMWA_NONCLIENT_RTL_LAYOUT,        // [set] Is non-client content RTL mirrored
			DWMWA_FORCE_ICONIC_REPRESENTATION, // [set] Force this window to display iconic thumbnails.
			DWMWA_FLIP3D_POLICY,               // [set] Designates how Flip3D will treat the window.
			DWMWA_EXTENDED_FRAME_BOUNDS,       // [get] Gets the extended frame bounds rectangle in screen space
			DWMWA_HAS_ICONIC_BITMAP,           // [set] Indicates an available bitmap when there is no better thumbnail representation.
			DWMWA_DISALLOW_PEEK,               // [set] Don't invoke Peek on the window.
			DWMWA_EXCLUDED_FROM_PEEK,          // [set] LivePreview exclusion information
			DWMWA_CLOAK,                       // [set] Cloak or uncloak the window
			DWMWA_CLOAKED,                     // [get] Gets the cloaked state of the window
			DWMWA_FREEZE_REPRESENTATION,       // [set] Force this window to freeze the thumbnail without live update
			DWMWA_LAST
		}

		private const int S_OK = 0x0;

		#endregion

		#region Win32 (for Win10)

		/// <summary>
		/// Sets window composition attribute (Undocumented API).
		/// </summary>
		/// <param name="hwnd">Window handle</param>
		/// <param name="data">Attribute data</param>
		/// <returns>True if successfully sets</returns>
		/// <remarks>
		/// This API and relevant parameters are derived from:
		/// https://github.com/riverar/sample-win10-aeroglass 
		/// </remarks>
		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowCompositionAttribute(
			IntPtr hwnd,
			ref WindowCompositionAttributeData data);

		[StructLayout(LayoutKind.Sequential)]
		private struct WindowCompositionAttributeData
		{
			public WindowCompositionAttribute Attribute;
			public IntPtr Data;
			public int SizeOfData;
		}

		private enum WindowCompositionAttribute
		{
			// ...
			WCA_ACCENT_POLICY = 19
			// ...
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct AccentPolicy
		{
			public AccentState AccentState;
			public int AccentFlags;
			public uint GradientColor;
			public int AnimationId;
		}

		private enum AccentState
		{
			ACCENT_DISABLED = 0,
			ACCENT_ENABLE_GRADIENT = 1,
			ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
			ACCENT_ENABLE_BLURBEHIND = 3,
			ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
			ACCENT_INVALID_STATE = 5
		}

		#endregion

		#region Win32 (for Win7)

		[DllImport("Dwmapi.dll")]
		private static extern int DwmIsCompositionEnabled(out bool pfEnabled);

		[DllImport("Dwmapi.dll")]
		private static extern int DwmEnableBlurBehindWindow(
			IntPtr hWnd,
			[In] ref DWM_BLURBEHIND pBlurBehind);

		[StructLayout(LayoutKind.Sequential)]
		private struct DWM_BLURBEHIND
		{
			public DWM_BB dwFlags;

			[MarshalAs(UnmanagedType.Bool)]
			public bool fEnable;

			public IntPtr hRgnBlur;

			[MarshalAs(UnmanagedType.Bool)]
			public bool fTransitionOnMaximized;
		}

		[Flags]
		private enum DWM_BB : uint
		{
			DWM_BB_ENABLE = 0x00000001,
			DWM_BB_BLURREGION = 0x00000002,
			DWM_BB_TRANSITIONONMAXIMIZED = 0x00000004
		}

		#endregion

		public static IReadOnlyCollection<string> Options => ColorPairs.Keys.Prepend(ThemeOption).Prepend(TextureOption).ToArray();

		private const string ThemeOption = "/theme";

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

		private static Texture _texture = Texture.Thick; // Default

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

		private static Dictionary<ColorElement, Brush> _colors;

		public static void ChangeTheme()
		{
			//const string DarkThemeUriString = @"/Monitorian.Core;component/Views/Themes/DarkTheme.xaml";
			const string LightThemeUriString = @"/Monitorian.Core;component/Views/Themes/LightTheme.xaml";

			ColorTheme? theme = null;

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
			while (i < arguments.Count - 1)
			{
				if (arguments[i] == ThemeOption)
				{
					if (Enum.TryParse(arguments[i + 1], true, out ColorTheme buffer))
						theme = buffer;
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
				i++;
			}

			if ((theme ?? UIInformation.GetWindowsTheme()) == ColorTheme.Light)
				ApplyResource(LightThemeUriString);

			_colors = colors.Any() ? colors : null;
		}

		private static void ApplyResource(string newUriString, string oldUriString = null)
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

		private static bool ChangeColors(Window window)
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

		#region Transitions

		public static bool DisableTransitions(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;
			var value = true;

			return (DwmSetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_TRANSITIONS_FORCEDISABLED,
				ref value,
				(uint)Marshal.SizeOf<bool>()) == S_OK);
		}

		#endregion

		#region Translucency

		private static readonly Lazy<bool> IsTransparencyEnabledForWin10 = new(() => IsEnableTransparencyOn());
		private static readonly Lazy<bool> IsTransparencyEnabledForWin7 = new(() => IsColorizationOpaqueBlendOn());

		private const string TranslucentBrushKey = "App.Background.Translucent";
		private static SolidColorBrush TranslucentBrush;
		private static Color? TranslucentColor;

		public static bool EnableBackgroundTranslucency(Window window)
		{
			if (ChangeColors(window) || (_texture == Texture.None))
				return false;

			if (OsVersion.Is10Threshold1OrNewer)
			{
				// For Windows 10
				if (!IsTransparencyEnabledForWin10.Value)
					return false;

				if (TranslucentBrush is null)
				{
					TranslucentBrush = (SolidColorBrush)window.FindResource(TranslucentBrushKey);

					if (_texture == Texture.Thick)
					{
						var color = TranslucentBrush.Color;
						TranslucentBrush = new SolidColorBrush(Color.FromArgb(a: 1, r: color.R, g: color.G, b: color.B));
						TranslucentColor = Color.FromArgb(a: (byte)Math.Max(0, color.A - 1), r: color.R, g: color.G, b: color.B);
					}
				}

				window.Background = TranslucentBrush;

				return EnableBackgroundBlurForWin10(window, TranslucentColor);
			}

			if (OsVersion.Is8OrNewer)
			{
				// For Windows 8 and 8.1, no blur effect is available.
				return false;
			}

			if (OsVersion.IsVistaOrNewer)
			{
				// For Windows 7
				if (!IsTransparencyEnabledForWin7.Value)
					return false;

				TranslucentBrush ??= (SolidColorBrush)window.FindResource(TranslucentBrushKey);

				window.Background = TranslucentBrush;

				return EnableBackgroundBlurForWin7(window);
			}

			return false;
		}

		private static bool EnableBackgroundBlurForWin10(Window window, Color? color = null)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			var accent = (color is null)
				? new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND }
				: new AccentPolicy
				{
					AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
					AccentFlags = 2,
					GradientColor = color.Value.ToUInt32()
				};

			var accentSize = Marshal.SizeOf(accent);

			var accentPointer = IntPtr.Zero;
			try
			{
				accentPointer = Marshal.AllocHGlobal(accentSize);
				Marshal.StructureToPtr(accent, accentPointer, false);

				var data = new WindowCompositionAttributeData
				{
					Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
					Data = accentPointer,
					SizeOfData = accentSize
				};

				return SetWindowCompositionAttribute(
					windowHandle,
					ref data);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to set window composition attribute." + Environment.NewLine
					+ ex);
				return false;
			}
			finally
			{
				Marshal.FreeHGlobal(accentPointer);
			}
		}

		private static bool EnableBackgroundBlurForWin7(Window window)
		{
			if ((DwmIsCompositionEnabled(out bool isEnabled) != S_OK) || !isEnabled)
				return false;

			var windowHandle = new WindowInteropHelper(window).Handle;

			var bb = new DWM_BLURBEHIND
			{
				dwFlags = DWM_BB.DWM_BB_ENABLE,
				fEnable = true,
				hRgnBlur = IntPtr.Zero
			};

			return (DwmEnableBlurBehindWindow(
				windowHandle,
				ref bb) == S_OK);
		}

		private static bool IsEnableTransparencyOn()
		{
			const string keyName = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
			const string valueName = "EnableTransparency";

			using var key = Registry.CurrentUser.OpenSubKey(keyName);

			return key?.GetValue(valueName) switch
			{
				0 => false, // Off
				1 => true,  // On
				_ => false
			};
		}

		private static bool IsColorizationOpaqueBlendOn()
		{
			const string keyName = @"Software\Microsoft\Windows\DWM";
			const string valueName = "ColorizationOpaqueBlend";

			using var key = Registry.CurrentUser.OpenSubKey(keyName);

			return key?.GetValue(valueName) switch
			{
				0 => true,  // On
				1 => false, // Off
				_ => false
			};
		}

		#endregion
	}
}