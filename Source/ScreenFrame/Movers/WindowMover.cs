using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using ScreenFrame.Helper;

namespace ScreenFrame.Movers
{
	/// <summary>
	/// Mover of <see cref="System.Windows.Window"/>
	/// </summary>
	public abstract class WindowMover
	{
		/// <summary>
		/// Window to be moved
		/// </summary>
		protected readonly Window _window;

		/// <summary>
		/// Per-Monitor DPI of window
		/// </summary>
		public DpiScale Dpi => VisualTreeHelperAddition.GetDpi(_window);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="window">Window to be moved</param>
		public WindowMover(Window window)
		{
			this._window = window ?? throw new ArgumentNullException(nameof(window));
			this._window.SourceInitialized += OnSourceInitialized;
			this._window.Closed += OnClosed;
			this._window.DpiChanged += OnDpiChanged;
		}

		private HwndSource _source;

		private void OnSourceInitialized(object sender, EventArgs e)
		{
			_source = PresentationSource.FromVisual(_window) as HwndSource;
			_source?.AddHook(WndProc);

			var dpi = VisualTreeHelperAddition.GetDpi(_window);
			if (!dpi.Equals(VisualTreeHelperAddition.SystemDpi))
			{
				AdjustWindow(dpi);
			}
		}

		private void OnClosed(object sender, EventArgs e)
		{
			_source?.RemoveHook(WndProc);
		}

		private void OnDpiChanged(object sender, DpiChangedEventArgs e)
		{
			AdjustWindow(e.NewDpi);
		}

		/// <summary>
		/// Adjusts DPI of window.
		/// </summary>
		/// <param name="dpi">DPI information</param>
		protected virtual void AdjustWindow(DpiScale dpi)
		{
			if (!OsVersion.Is81OrNewer || OsVersion.Is10Redstone1OrNewer)
				return;

			if (_window.Content is FrameworkElement content)
			{
				content.LayoutTransform = dpi.IsDefault()
					? Transform.Identity
					: new ScaleTransform(dpi.DpiScaleX, dpi.DpiScaleY);
			}
		}

		private const int WM_WINDOWPOSCHANGING = 0x0046;
		private const int WM_WINDOWPOSCHANGED = 0x0047;
		private const int WM_DPICHANGED = 0x02E0;
		private const int WM_DISPLAYCHANGE = 0x007E;
		private const int WM_ACTIVATEAPP = 0x001C;
		private const int WM_KEYDOWN = 0x0100;

		/// <summary>
		/// Handles window messages.
		/// </summary>
		protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_WINDOWPOSCHANGING:
					HandleWindowPosChanging(hwnd, msg, wParam, lParam, ref handled);
					break;

				case WM_WINDOWPOSCHANGED:
					HandleWindowPosChanged(hwnd, msg, wParam, lParam, ref handled);
					break;

				case WM_DPICHANGED:
					HandleDpiChanged(hwnd, msg, wParam, lParam, ref handled);
					break;

				case WM_DISPLAYCHANGE:
					HandleDisplayChange(hwnd, msg, wParam, lParam, ref handled);
					break;

				case WM_ACTIVATEAPP:
					HandleActivateApp(hwnd, msg, wParam, lParam, ref handled);
					break;

				case WM_KEYDOWN:
					HandleKeyDown(hwnd, msg, wParam, lParam, ref handled);
					break;
			}
			return IntPtr.Zero;
		}

		/// <summary>
		/// Handles window position changing event.
		/// </summary>
		protected abstract void HandleWindowPosChanging(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

		/// <summary>
		/// Handles window position changed event.
		/// </summary>
		protected abstract void HandleWindowPosChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

		/// <summary>
		/// Handles DPI changed event.
		/// </summary>
		protected virtual void HandleDpiChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (OsVersion.Is10Redstone1OrNewer)
				return;

			if (_window.SizeToContent != SizeToContent.WidthAndHeight)
			{
				var windowRect = VisualTreeHelperAddition.ConvertToRect(lParam);
				WindowHelper.SetWindowPosition(_window, windowRect);
			}

			var dpi = VisualTreeHelperAddition.ConvertToDpiScale(wParam);
			VisualTreeHelper.SetRootDpi(_window, dpi);
			handled = true;
		}

		/// <summary>
		/// Handles display change event.
		/// </summary>
		protected abstract void HandleDisplayChange(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

		/// <summary>
		/// Occurs when the application to which the window belongs is activated.
		/// </summary>
		public event EventHandler AppActivated;

		/// <summary>
		/// Occurs when the application to which the window belongs is deactivated. 
		/// </summary>
		public event EventHandler AppDeactivated;

		/// <summary>
		/// Handles application activated/deactivated event.
		/// </summary>
		protected virtual void HandleActivateApp(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			var isActivated = Convert.ToBoolean(wParam.ToInt32());
			if (isActivated)
			{
				AppActivated?.Invoke(_window, EventArgs.Empty);
			}
			else
			{
				AppDeactivated?.Invoke(_window, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs when the escape key is pressed.
		/// </summary>
		public event EventHandler EscapeKeyDown;

		private const int VK_ESCAPE = 0x1B;

		/// <summary>
		/// Handles key pressed event.
		/// </summary>
		protected virtual void HandleKeyDown(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (wParam.ToInt32())
			{
				case VK_ESCAPE:
					EscapeKeyDown?.Invoke(_window, EventArgs.Empty);
					break;
			}
		}

		/// <summary>
		/// Determines whether the window is foreground window.
		/// </summary>
		/// <returns>True if foreground</returns>
		public bool IsForeground() => WindowHelper.IsForegroundWindow(_window);
	}
}