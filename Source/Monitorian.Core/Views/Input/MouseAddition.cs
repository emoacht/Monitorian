using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Monitorian.Core.Views.Input
{
	/// <summary>
	/// Additional events to <see cref="System.Windows.Input.Mouse"/>
	/// </summary>
	public static class MouseAddition
	{
		#region Event

		/// <summary>
		/// Mouse horizontal wheel event
		/// </summary>
		public static readonly RoutedEvent MouseHorizontalWheelEvent = EventManager.RegisterRoutedEvent(
			"MouseHorizontalWheel",
			RoutingStrategy.Bubble,
			typeof(MouseWheelEventHandler),
			typeof(MouseAddition));

		public static void AddMouseHorizontalWheelHandler(DependencyObject d, MouseWheelEventHandler handler)
		{
			if (d is UIElement element)
				element.AddHandler(MouseHorizontalWheelEvent, handler);
		}

		public static void RemoveMouseHorizontalWheelHandler(DependencyObject d, MouseWheelEventHandler handler)
		{
			if (d is UIElement element)
				element.RemoveHandler(MouseHorizontalWheelEvent, handler);
		}

		#endregion

		private static readonly HashSet<IntPtr> _windows = new();

		/// <summary>
		/// Enables mouse horizontal wheel event.
		/// </summary>
		/// <param name="element">UIElement</param>
		public static void EnableMouseHorizontalWheel(UIElement element)
		{
			var window = element as Window ?? Window.GetWindow(element) ?? throw new ArgumentNullException(nameof(element));

			if (window.IsLoaded)
			{
				AddMouseHorizontalWheelHook(window);
			}
			else
			{
				window.Loaded += OnLoaded;
			}

			static void OnLoaded(object sender, RoutedEventArgs e)
			{
				var window = (Window)sender;
				window.Loaded -= OnLoaded;
				AddMouseHorizontalWheelHook(window);
			}
		}

		private static void AddMouseHorizontalWheelHook(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;
			if (_windows.Add(handle) && (HwndSource.FromHwnd(handle) is HwndSource source))
			{
				source.AddHook(WndProc);
				window.Closed += OnClosed;
			}

			static void OnClosed(object sender, EventArgs e)
			{
				var window = (Window)sender;
				window.Closed -= OnClosed;
				RemoveMouseHorizontalWheelHook(window);
			}
		}

		/// <summary>
		/// Disables mouse horizontal wheel event.
		/// </summary>
		/// <param name="window">Window</param>
		/// <remarks>
		/// This method only accepts Window instance for simplicity.
		/// </remarks>
		public static void DisableMouseHorizontalWheel(Window window)
		{
			if (window is null)
				throw new ArgumentNullException(nameof(window));

			RemoveMouseHorizontalWheelHook(window);
		}

		private static void RemoveMouseHorizontalWheelHook(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;
			if (_windows.Remove(handle) && (HwndSource.FromHwnd(handle) is HwndSource source))
			{
				source.RemoveHook(WndProc);
			}
		}

		private const int WM_MOUSEHWHEEL = 0x020E;

		private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_MOUSEHWHEEL:
					HandleMouseHorizontalWheel(hwnd, msg, wParam, lParam, ref handled);
					break;
			}
			return IntPtr.Zero;
		}

		private static void HandleMouseHorizontalWheel(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			int delta = -unchecked((short)((long)wParam >> 16)); // delta needs to be reversed.
			if (delta is 0)
				return;

			var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta) { RoutedEvent = MouseHorizontalWheelEvent };
			Mouse.DirectlyOver?.RaiseEvent(args);
		}
	}
}