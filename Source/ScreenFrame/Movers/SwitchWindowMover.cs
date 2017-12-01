using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

using ScreenFrame.Helper;

namespace ScreenFrame.Movers
{
	/// <summary>
	/// Window mover which implements functions for switch window
	/// </summary>
	public class SwitchWindowMover : StickWindowMover
	{
		/// <summary>
		/// Whether the window is departed from taskbar
		/// </summary>
		public bool IsDeparted { get; private set; }

		/// <summary>
		/// Time interval before departure
		/// </summary>
		public TimeSpan DepartureInterval
		{
			get => _departureInterval;
			set
			{
				if (value < TimeSpan.Zero)
					return;

				_departureInterval = value;
			}
		}
		private TimeSpan _departureInterval = TimeSpan.FromMilliseconds(100); // Default

		private readonly DispatcherTimer _departureTimer;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="window">Window to be moved</param>
		/// <param name="notifyIcon">NotifyIcon to be referred</param>
		public SwitchWindowMover(Window window, NotifyIcon notifyIcon) : base(window, notifyIcon)
		{
			_departureTimer = new DispatcherTimer();
			_departureTimer.Tick += (sender, e) =>
			{
				_departureTimer.Stop();
				InitiateDeparture();
			};
		}

		private const int WM_ENTERSIZEMOVE = 0x0231;
		private const int WM_EXITSIZEMOVE = 0x0232;
		private const int WM_MOVE = 0x0003;

		/// <summary>
		/// Handles window messages.
		/// </summary>
		protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_ENTERSIZEMOVE:
					//Debug.WriteLine(nameof(WM_ENTERSIZEMOVE));
					_departureTimer.Interval = DepartureInterval;
					_departureTimer.Start();
					break;

				case WM_EXITSIZEMOVE:
					//Debug.WriteLine(nameof(WM_EXITSIZEMOVE));
					_departureTimer.Stop();
					CheckDeparture();
					break;

				case WM_MOVE:
					//Debug.WriteLine(nameof(WM_MOVE));
					CheckDeparture();
					break;
			}
			return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
		}

		private void InitiateDeparture()
		{
			IsDeparted = true;
		}

		private void CheckDeparture()
		{
			if (!IsDeparted)
				return;

			if (!WindowHelper.TryGetTaskbar(out Rect taskbarRect, out TaskbarAlignment _))
				return;

			var windowRect = WindowHelper.GetDwmWindowRect(_window);
			if (!windowRect.IntersectsWith(taskbarRect))
				return;

			IsDeparted = false;
		}

		/// <summary>
		/// Tries to get the adjacent location using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if succeeded</returns>
		protected override bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Point location)
		{
			if (IsDeparted)
			{
				location = default(Point);
				return false;
			}
			return base.TryGetAdjacentLocation(windowWidth, windowHeight, out location);
		}

		/// <summary>
		/// Handles DPI changed event.
		/// </summary>
		protected override void HandleDpiChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (OsVersion.Is10Redstone1OrNewer)
				return;

			if (IsDeparted)
			{
				var windowRect = VisualTreeHelperAddition.ConvertToRect(lParam);
				WindowHelper.SetWindowPosition(_window, windowRect);
			}
			base.HandleDpiChanged(hwnd, msg, wParam, lParam, ref handled);
		}
	}
}