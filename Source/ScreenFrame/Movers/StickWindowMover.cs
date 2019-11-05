using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ScreenFrame.Movers
{
	/// <summary>
	/// Mover of <see cref="System.Windows.Window"/> which implements functions for stick window
	/// </summary>
	public class StickWindowMover : BasicWindowMover
	{
		private readonly NotifyIcon _notifyIcon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="window">Window to be moved</param>
		/// <param name="notifyIcon">NotifyIcon to be referred</param>
		public StickWindowMover(Window window, NotifyIcon notifyIcon) : base(window)
		{
			this._notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
		}

		/// <summary>
		/// Alignment of pivot
		/// </summary>
		public override PivotAlignment PivotAlignment { get; protected set; }

		/// <summary>
		/// Attempts to get the adjacent location using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if successfully gets</returns>
		protected override bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Rect location) =>
			TryGetAdjacentLocationToTaskbar(windowWidth, windowHeight, out location);

		private enum IconPlacement
		{
			/// <summary>
			/// The location of NotifyIcon is unknown.
			/// </summary>
			Unknown = 0,

			/// <summary>
			/// NotifyIcon locates in the taskbar.
			/// </summary>
			InTaskbar,

			/// <summary>
			/// NotifyIcon locates in the notification overflow area.
			/// </summary>
			InOverflowArea
		}

		/// <summary>
		/// Attempts to get the adjacent location to NotifyIcon using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if successfully gets</returns>
		protected bool TryGetAdjacentLocationToTaskbar(double windowWidth, double windowHeight, out Rect location)
		{
			if (!WindowHelper.TryGetTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment))
			{
				location = default;
				return false;
			}

			var iconPlacement = IconPlacement.Unknown;
			var overflowAreaRect = default(Rect);

			if (NotifyIconHelper.TryGetNotifyIconRect(_notifyIcon, out Rect iconRect))
			{
				if (taskbarRect.Contains(iconRect))
				{
					iconPlacement = IconPlacement.InTaskbar;
				}
				else if (WindowHelper.TryGetOverflowAreaRect(out overflowAreaRect))
				{
					iconPlacement = IconPlacement.InOverflowArea;
				}
			}

			if (!WindowHelper.TryGetDwmWindowMargin(_window, out Thickness windowMargin))
				windowMargin = new Thickness(0); // Fallback

			double x = 0, y = 0;

			switch (taskbarAlignment)
			{
				case TaskbarAlignment.Top:
				case TaskbarAlignment.Bottom:
					switch (iconPlacement)
					{
						case IconPlacement.InTaskbar:
							x = iconRect.Right;
							break;
						case IconPlacement.InOverflowArea:
							x = overflowAreaRect.Left;
							break;
						default:
							x = taskbarRect.Right; // Fallback
							break;
					}
					x -= (windowWidth - windowMargin.Right);

					switch (taskbarAlignment)
					{
						case TaskbarAlignment.Top:
							y = taskbarRect.Bottom - windowMargin.Top;
							PivotAlignment = PivotAlignment.TopRight;
							break;
						case TaskbarAlignment.Bottom:
							y = taskbarRect.Top - (windowHeight - windowMargin.Bottom);
							PivotAlignment = PivotAlignment.BottomRight;
							break;
					}
					break;
				case TaskbarAlignment.Left:
				case TaskbarAlignment.Right:
					switch (taskbarAlignment)
					{
						case TaskbarAlignment.Left:
							x = taskbarRect.Right - windowMargin.Left;
							PivotAlignment = PivotAlignment.BottomLeft;
							break;
						case TaskbarAlignment.Right:
							x = taskbarRect.Left - (windowWidth - windowMargin.Right);
							PivotAlignment = PivotAlignment.BottomRight;
							break;
					}

					switch (iconPlacement)
					{
						case IconPlacement.InTaskbar:
							y = iconRect.Bottom;
							break;
						case IconPlacement.InOverflowArea:
							y = overflowAreaRect.Top;
							break;
						default:
							y = taskbarRect.Bottom; // Fallback
							break;
					}
					y -= (windowHeight - windowMargin.Bottom);
					break;
			}
			location = new Rect(x, y, windowWidth, windowHeight);
			return true;
		}
	}
}