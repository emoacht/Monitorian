using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Monitorian.Core.Views.Input;

/// <summary>
/// Additional attached events to <see cref="System.Windows.Input.Mouse"/>
/// </summary>
public static class MouseAddition
{
	#region Event

	/// <summary>
	/// Mouse horizontal wheel attached event
	/// </summary>
	public static readonly RoutedEvent MouseHorizontalWheelEvent = EventManager.RegisterRoutedEvent(
		"MouseHorizontalWheel",
		RoutingStrategy.Bubble,
		typeof(MouseWheelEventHandler),
		typeof(MouseAddition));

	public static void AddMouseHorizontalWheelHandler(DependencyObject d, MouseWheelEventHandler handler)
	{
		if (d is UIElement element)
		{
			element.AddHandler(MouseHorizontalWheelEvent, handler);
			EnableMouseHorizontalWheel(element);
		}
	}

	public static void RemoveMouseHorizontalWheelHandler(DependencyObject d, MouseWheelEventHandler handler)
	{
		if (d is UIElement element)
		{
			element.RemoveHandler(MouseHorizontalWheelEvent, handler);
			DisableMouseHorizontalWheel(element);
		}
	}

	#endregion

	private static readonly Dictionary<Window, HashSet<UIElement>> _windows = new();

	/// <summary>
	/// Enables mouse horizontal wheel attached event for a specified UIElement.
	/// </summary>
	/// <param name="element">UIElement that listens to this event</param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void EnableMouseHorizontalWheel(UIElement element)
	{
		var window = element as Window ?? Window.GetWindow(element) ?? throw new ArgumentNullException(nameof(element));
		if (window.IsLoaded)
		{
			AddMouseHorizontalWheelHook(window, element);
		}
		else
		{
			window.Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			var window = (Window)sender;
			window.Loaded -= OnLoaded;
			AddMouseHorizontalWheelHook(window, element);
		}
	}

	private static void AddMouseHorizontalWheelHook(Window window, UIElement element)
	{
		if (_windows.TryGetValue(window, out HashSet<UIElement> elements))
		{
			elements.Add(element);
			return;
		}

		_windows.Add(window, new HashSet<UIElement> { element });

		var handle = new WindowInteropHelper(window).Handle;
		if ((handle != IntPtr.Zero) && (HwndSource.FromHwnd(handle) is HwndSource source))
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
	/// Disables mouse horizontal wheel attached event for a specified UIElement.
	/// </summary>
	/// <param name="element">UIElement that listens to this event</param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void DisableMouseHorizontalWheel(UIElement element)
	{
		var window = element as Window ?? Window.GetWindow(element) ?? throw new ArgumentNullException(nameof(element));
		RemoveMouseHorizontalWheelHook(window, element);
	}

	private static void RemoveMouseHorizontalWheelHook(Window window, UIElement element = null)
	{
		if (element is not null)
		{
			if (_windows.TryGetValue(window, out HashSet<UIElement> elements))
			{
				if (!elements.Remove(element) || elements.Any())
					return;
			}
		}

		if (!_windows.Remove(window))
			return;

		var handle = new WindowInteropHelper(window).Handle;
		if ((handle != IntPtr.Zero) && (HwndSource.FromHwnd(handle) is HwndSource source))
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
		int delta = unchecked((short)((long)wParam >> 16));
		if (delta is 0)
			return;

		var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta) { RoutedEvent = MouseHorizontalWheelEvent };
		Mouse.DirectlyOver?.RaiseEvent(args);
	}
}