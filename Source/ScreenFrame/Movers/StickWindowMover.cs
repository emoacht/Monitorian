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
		/// Whether to keep distance from the taskbar or the notification overflow area
		/// </summary>
		/// <remarks>This is valid only on Windows 11.</remarks>
		public bool KeepsDistance { get; set; }

		/// <summary>
		/// Distance from the taskbar or the notification overflow area
		/// </summary>
		/// <remarks>This is valid only on Windows 11.</remarks>
		public double Distance { get; set; } = 12D;

		/// <summary>
		/// Attempts to get the adjacent location to NotifyIcon using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if successfully gets</returns>
		protected bool TryGetAdjacentLocationToTaskbar(double windowWidth, double windowHeight, out Rect location)
		{
			if (!WindowHelper.TryGetTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment, out bool isShown))
			{
				location = default;
				return false;
			}

			var iconPlacement = IconPlacement.Unknown;
			var overflowAreaRect = default(Rect);
			var isMarginIncluded = false;

			if (NotifyIconHelper.TryGetNotifyIconRect(_notifyIcon, out Rect iconRect))
			{
				if (taskbarRect.Contains(
					iconRect.X + iconRect.Width / 2D,
					iconRect.Y + iconRect.Height / 2D))
				{
					iconPlacement = IconPlacement.InTaskbar;
				}
				else if (WindowHelper.TryGetOverflowAreaRect(out overflowAreaRect, out bool buffer)
					&& overflowAreaRect.Contains(iconRect))
				{
					iconPlacement = IconPlacement.InOverflowArea;
					isMarginIncluded = buffer;
				}
			}

			if (!WindowHelper.TryGetDwmWindowMargin(_window, out Thickness windowMargin))
				windowMargin = new Thickness(0); // Fallback

			var isLeftToRight = !CultureInfoAddition.UserDefaultUICulture.TextInfo.IsRightToLeft;

			var distance = new Vector(0, 0);
			if (OsVersion.Is11OrGreater && KeepsDistance)
			{
				distance = (OsVersion.Is11Build22623OrGreater && isMarginIncluded)
					? new Vector(0, Distance)
					: new Vector(Distance, Distance);
				distance *= VisualTreeHelperAddition.GetDpi(_window).ToMatrix();
			}

			double x = 0, y = 0;

			// To avoid a gap between window and taskbar when taskbar alignment is right or bottom
			// and monitor DPI is 125%, 150%, 175%, the window width and height (in DIP) must be
			// a multiple of 4. Otherwise, the window width and height multiplied with those DPI
			// will have a fraction and it will cause a blurry edge looking as if there is a gap.
			switch (taskbarAlignment)
			{
				case TaskbarAlignment.Top:
				case TaskbarAlignment.Bottom:
					x = iconPlacement switch
					{
						IconPlacement.InTaskbar => isLeftToRight ? iconRect.Right : iconRect.Left,
						IconPlacement.InOverflowArea => isLeftToRight ? (overflowAreaRect.Left - distance.X) : (overflowAreaRect.Right + distance.X),
						_ => isLeftToRight ? (taskbarRect.Right - distance.X) : (taskbarRect.Left + distance.X), // Fallback
					};
					x -= isLeftToRight ? (windowWidth - windowMargin.Right) : windowMargin.Left;

					if (OsVersion.Is11Build22623OrGreater && WindowHelper.TryGetStartButtonRect(out Rect buttonRect))
						taskbarRect = new Rect(taskbarRect.Left, buttonRect.Top, taskbarRect.Width, buttonRect.Height);

					switch (taskbarAlignment)
					{
						case TaskbarAlignment.Top:
							y = (isShown ? taskbarRect.Bottom : taskbarRect.Top) - windowMargin.Top + distance.Y;
							PivotAlignment = isLeftToRight ? PivotAlignment.TopRight : PivotAlignment.TopLeft;
							break;
						case TaskbarAlignment.Bottom:
							y = (isShown ? taskbarRect.Top : taskbarRect.Bottom) - (windowHeight - windowMargin.Bottom) - distance.Y;
							PivotAlignment = isLeftToRight ? PivotAlignment.BottomRight : PivotAlignment.BottomLeft;
							break;
					}
					break;
				case TaskbarAlignment.Left:
				case TaskbarAlignment.Right:
					switch (taskbarAlignment)
					{
						case TaskbarAlignment.Left:
							x = (isShown ? taskbarRect.Right : taskbarRect.Left) - windowMargin.Left + distance.X;
							PivotAlignment = PivotAlignment.BottomLeft;
							break;
						case TaskbarAlignment.Right:
							x = (isShown ? taskbarRect.Left : taskbarRect.Right) - (windowWidth - windowMargin.Right) - distance.X;
							PivotAlignment = PivotAlignment.BottomRight;
							break;
					}

					y = iconPlacement switch
					{
						IconPlacement.InTaskbar => iconRect.Bottom,
						IconPlacement.InOverflowArea => overflowAreaRect.Top - distance.Y,
						_ => taskbarRect.Bottom - distance.Y, // Fallback
					};
					y -= (windowHeight - windowMargin.Bottom);
					break;
			}
			location = new Rect(x, y, windowWidth, windowHeight);
			return true;
		}
	}
}