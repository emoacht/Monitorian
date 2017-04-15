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
	/// Window mover which implements functions for stick window.
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
		/// Tries to get the adjacent location using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if succeeded</returns>
		protected override bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Point location) =>
			TryGetAdjacentLocationToTaskbar(windowWidth, windowHeight, out location);

		/// <summary>
		/// Tries to get the adjacent location to NotifyIcon using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if succeeded</returns>
		protected bool TryGetAdjacentLocationToTaskbar(double windowWidth, double windowHeight, out Point location)
		{
			if (!WindowHelper.TryGetTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment))
			{
				location = default(Point);
				return false;
			}

			if (!NotifyIconHelper.TryGetNotifyIconRect(_notifyIcon, out Rect iconRect))
				iconRect = taskbarRect; // Fallback

			var margin = WindowHelper.GetDwmWindowMargin(_window);
			if (margin == Padding.Empty)
				margin = new Padding(0); // Fallback

			double x = 0;
			double y = 0;

			switch (taskbarAlignment)
			{
				case TaskbarAlignment.Top:
				case TaskbarAlignment.Bottom:
					x = iconRect.Right - windowWidth + margin.Right;

					switch (taskbarAlignment)
					{
						case TaskbarAlignment.Top:
							y = taskbarRect.Bottom - margin.Top;
							break;
						case TaskbarAlignment.Bottom:
							y = taskbarRect.Top - windowHeight + margin.Bottom;
							break;
					}
					break;
				case TaskbarAlignment.Left:
				case TaskbarAlignment.Right:
					switch (taskbarAlignment)
					{
						case TaskbarAlignment.Left:
							x = taskbarRect.Right - margin.Left;
							break;
						case TaskbarAlignment.Right:
							x = taskbarRect.Left - windowWidth + margin.Right;
							break;
					}

					y = iconRect.Bottom - windowHeight + margin.Bottom;
					break;
			}
			location = new Point(x, y);
			return true;
		}
	}
}