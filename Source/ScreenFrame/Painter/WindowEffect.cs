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
using Microsoft.Win32;

using ScreenFrame.Helper;

namespace ScreenFrame.Painter
{
	internal static class WindowEffect
	{
		#region Win32 (common)

		[DllImport("Dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(
			IntPtr hwnd,
			uint dwAttribute,
			[In] ref bool pvAttribute, // IntPtr
			uint cbAttribute);

		[DllImport("Dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(
			IntPtr hwnd,
			uint dwAttribute,
			[In] ref uint pvAttribute, // IntPtr
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

			// Derived from dwmapi.h included in Windows Insider Preview SDK
			DWMWA_PASSIVE_UPDATE_MODE,            // [set] BOOL, Updates the window only when desktop composition runs for other reasons
			DWMWA_USE_HOSTBACKDROPBRUSH,          // [set] BOOL, Allows the use of host backdrop brushes for the window.
			DWMWA_USE_IMMERSIVE_DARK_MODE = 20,   // [set] BOOL, Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
			DWMWA_WINDOW_CORNER_PREFERENCE = 33,  // [set] WINDOW_CORNER_PREFERENCE, Controls the policy that rounds top-level window corners
			DWMWA_BORDER_COLOR,                   // [set] COLORREF, The color of the thin border around a top-level window
			DWMWA_CAPTION_COLOR,                  // [set] COLORREF, The color of the caption
			DWMWA_TEXT_COLOR,                     // [set] COLORREF, The color of the caption text
			DWMWA_VISIBLE_FRAME_BORDER_THICKNESS, // [get] UINT, width of the visible border around a thick frame window

			DWMWA_LAST
		}

		// Derived from dwmapi.h included in Windows Insider Preview SDK
		private enum DWMWCP : uint
		{
			DWMWCP_DEFAULT = 0,
			DWMWCP_DONOTROUND = 1,
			DWMWCP_ROUND = 2,
			DWMWCP_ROUNDSMALL = 3
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

		public static bool SetCornersForWin11(Window window, CornerPreference corner)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			uint value;
			switch (corner)
			{
				case CornerPreference.NotRound:
					value = (uint)DWMWCP.DWMWCP_DONOTROUND;
					break;
				case CornerPreference.Round:
					value = (uint)DWMWCP.DWMWCP_ROUND;
					break;
				default:
					return false;
			}

			return (DwmSetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_WINDOW_CORNER_PREFERENCE,
				ref value,
				(uint)Marshal.SizeOf(value)) == S_OK);
		}

		public static bool EnableBackgroundBlurForWin10(Window window, Color? color = null)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			var accent = (color is null)
				? new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND }
				: new AccentPolicy
				{
					AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
					AccentFlags = 2,
					GradientColor = color.Value.ToUInt32() // If 0, blur effect will not be added.
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

		public static bool EnableBackgroundBlurForWin7(Window window)
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

		public static bool IsTransparencyEnabledForWin10 => _isTransparencyEnabledForWin10.Value;
		private static readonly Lazy<bool> _isTransparencyEnabledForWin10 = new(() => IsEnableTransparencyOn());

		public static bool IsTransparencyEnabledForWin7 => _isTransparencyEnabledForWin7.Value;
		private static readonly Lazy<bool> _isTransparencyEnabledForWin7 = new(() => IsColorizationOpaqueBlendOn());

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
	}

	/// <summary>
	/// Corner preferences of window
	/// </summary>
	public enum CornerPreference
	{
		/// <summary>
		/// None
		/// </summary>
		None = 0,

		/// <summary>
		/// Not round
		/// </summary>
		NotRound,

		/// <summary>
		/// Round
		/// </summary>
		Round
	}
}