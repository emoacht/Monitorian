using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using ScreenFrame.Helper;

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
				if (taskbarRect.Contains(
					iconRect.X + iconRect.Width / 2D,
					iconRect.Y + iconRect.Height / 2D))
				{
					iconPlacement = IconPlacement.InTaskbar;
				}
				else if (WindowHelper.TryGetOverflowAreaRect(out overflowAreaRect)
					&& overflowAreaRect.Contains(iconRect))
				{
					iconPlacement = IconPlacement.InOverflowArea;
				}
			}

			if (!WindowHelper.TryGetDwmWindowMargin(_window, out Thickness windowMargin))
				windowMargin = new Thickness(0); // Fallback

			var isLeftToRight = !CultureInfoAddition.UserDefaultUICulture.TextInfo.IsRightToLeft;

			double x = 0, y = 0;

			switch (taskbarAlignment)
			{
				case TaskbarAlignment.Top:
				case TaskbarAlignment.Bottom:
					x = iconPlacement switch
					{
						IconPlacement.InTaskbar => isLeftToRight ? iconRect.Right : iconRect.Left,
						IconPlacement.InOverflowArea => isLeftToRight ? overflowAreaRect.Left : overflowAreaRect.Right,
						_ => isLeftToRight ? taskbarRect.Right : taskbarRect.Left, // Fallback
					};
					x -= isLeftToRight ? (windowWidth - windowMargin.Right) : windowMargin.Left;

					switch (taskbarAlignment)
					{
						case TaskbarAlignment.Top:
							y = taskbarRect.Bottom - windowMargin.Top;
							PivotAlignment = isLeftToRight ? PivotAlignment.TopRight : PivotAlignment.TopLeft;
							break;
						case TaskbarAlignment.Bottom:
							y = taskbarRect.Top - (windowHeight - windowMargin.Bottom);
							PivotAlignment = isLeftToRight ? PivotAlignment.BottomRight : PivotAlignment.BottomLeft;
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

					y = iconPlacement switch
					{
						IconPlacement.InTaskbar => iconRect.Bottom,
						IconPlacement.InOverflowArea => overflowAreaRect.Top,
						_ => taskbarRect.Bottom, // Fallback
					};
					y -= (windowHeight - windowMargin.Bottom);
					break;
			}
			location = new Rect(x, y, windowWidth, windowHeight);
			return true;
		}
	}
}