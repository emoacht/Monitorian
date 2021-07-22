using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ScreenFrame.Movers
{
	/// <summary>
	/// Mover of <see cref="System.Windows.Window"/> which implements functions for switch window
	/// </summary>
	public class SwitchWindowMover : StickWindowMover
	{
		/// <summary>
		/// Whether the window is departed from taskbar
		/// </summary>
		public bool IsDeparted
		{
			get => _isDeparted;
			private set
			{
				_isDeparted = value;
				IsDepartedChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		private bool _isDeparted;

		/// <summary>
		/// Occurs when IsDeparted is changed.
		/// </summary>
		public event EventHandler IsDepartedChanged;

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

		/// <summary>
		/// Alignment of pivot
		/// </summary>
		public override PivotAlignment PivotAlignment
		{
			get => IsDeparted ? PivotAlignment.None : base.PivotAlignment;
			protected set => base.PivotAlignment = value;
		}

		private const int WM_ENTERSIZEMOVE = 0x0231;
		private const int WM_EXITSIZEMOVE = 0x0232;
		private const int WM_MOVE = 0x0003;
		private const int WM_SIZE = 0x0005;

		/// <summary>
		/// Handles window messages.
		/// </summary>
		protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_ENTERSIZEMOVE:
					_departureTimer.Interval = DepartureInterval;
					_departureTimer.Start();
					break;

				case WM_EXITSIZEMOVE:
					_departureTimer.Stop();
					CheckDeparture();
					break;

				case WM_MOVE:
				case WM_SIZE:
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

			if (!WindowHelper.TryGetTaskbar(out Rect taskbarRect, out _, out _))
				return;

			if (!WindowHelper.TryGetDwmWindowRect(_window, out Rect windowRect))
				return;

			if (!windowRect.IntersectsWith(taskbarRect))
				return;

			IsDeparted = false;
		}

		/// <summary>
		/// Attempts to get the adjacent location using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if successfully gets</returns>
		protected override bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Rect location)
		{
			if (IsDeparted)
			{
				location = default;
				return false;
			}
			return base.TryGetAdjacentLocation(windowWidth, windowHeight, out location);
		}
	}
}