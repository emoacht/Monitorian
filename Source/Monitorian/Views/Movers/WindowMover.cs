using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using Monitorian.Helper;

namespace Monitorian.Views.Movers
{
	internal abstract class WindowMover
	{
		protected readonly Window _window;

		public WindowMover(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			this._window = window;
			this._window.SourceInitialized += OnSourceInitialized;
			this._window.Closed += OnClosed;
			this._window.DpiChanged += OnDpiChanged;
		}

		private HwndSource _Source;

		private void OnSourceInitialized(object sender, EventArgs e)
		{
			_Source = PresentationSource.FromVisual(_window) as HwndSource;
			_Source?.AddHook(WndProc);

			var dpi = VisualTreeHelperAddition.GetDpi(_window);
			if (!dpi.Equals(VisualTreeHelperAddition.SystemDpi))
			{
				AdjustWindow(dpi);
			}
		}

		private void OnClosed(object sender, EventArgs e)
		{
			_Source?.RemoveHook(WndProc);
		}

		private void OnDpiChanged(object sender, DpiChangedEventArgs e)
		{
			AdjustWindow(e.NewDpi);
		}

		protected virtual void AdjustWindow(DpiScale dpi)
		{
			if (!OsVersion.Is81OrNewer || OsVersion.Is10Redstone1OrNewer)
				return;

			var content = _window.Content as FrameworkElement;
			if (content != null)
			{
				content.LayoutTransform = dpi.IsDefault()
					? Transform.Identity
					: new ScaleTransform(dpi.DpiScaleX, dpi.DpiScaleY);
			}
		}

		private const int WM_WINDOWPOSCHANGING = 0x0046;
		private const int WM_WINDOWPOSCHANGED = 0x0047;
		private const int WM_DPICHANGED = 0x02E0;

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_WINDOWPOSCHANGING:
					//Debug.WriteLine("WM_WINDOWPOSCHANGING");
					HandleWindowPosChanging(hwnd, msg, wParam, lParam, ref handled);
					break;

				case WM_WINDOWPOSCHANGED:
					//Debug.WriteLine("WM_WINDOWPOSCHANGED");
					HandleWindowPosChanged(hwnd, msg, wParam, lParam, ref handled);
					break;

				case WM_DPICHANGED:
					//Debug.WriteLine("WM_DPICHANGED");
					HandleDpiChanged(hwnd, msg, wParam, lParam, ref handled);
					break;
			}
			return IntPtr.Zero;
		}

		protected abstract void HandleWindowPosChanging(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

		protected abstract void HandleWindowPosChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

		protected virtual void HandleDpiChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (!OsVersion.Is10Redstone1OrNewer)
			{
				var dpi = DpiScaleExtension.FromUInt((uint)wParam);
				VisualTreeHelper.SetRootDpi(_window, dpi);
				handled = true;
			}
		}
	}
}