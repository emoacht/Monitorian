﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using ScreenFrame.Helper;

namespace ScreenFrame.Movers
{
	/// <summary>
	/// Mover of <see cref="System.Windows.Window"/> which implements functions for popup window
	/// </summary>
	public class PopupWindowMover : BasicWindowMover
	{
		private readonly Point _pivot;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="window">Window to be moved</param>
		/// <param name="pivot">Pivot point to be referred</param>
		public PopupWindowMover(Window window, Point pivot) : base(window)
		{
			this._pivot = pivot;
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
			TryGetAdjacentLocationToPivot(windowWidth, windowHeight, out location);

		/// <summary>
		/// Attempts to get the adjacent location to pivot point using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if successfully gets</returns>
		protected bool TryGetAdjacentLocationToPivot(double windowWidth, double windowHeight, out Rect location)
		{
			if (!WindowHelper.TryGetMonitorRect(_pivot, out _, out Rect workRect))
			{
				location = default;
				return false;
			}

			var isLeftToRight = !CultureInfoAddition.UserDefaultUICulture.TextInfo.IsRightToLeft;

			PivotAlignment = isLeftToRight
				? PivotAlignment.TopLeft
				: PivotAlignment.TopRight;

			var x = _pivot.X;
			var y = _pivot.Y;

			switch (PivotAlignment)
			{
				case PivotAlignment.TopLeft:
					x -= 1;
					y -= 1;
					break;
				case PivotAlignment.TopRight:
					x -= (windowWidth - 1);
					y -= 1;
					break;
			}

			// Make sure the right-bottom corner of window is inside the work area of monitor.
			x = Math.Min(x + windowWidth, workRect.Right) - windowWidth;
			y = Math.Min(y + windowHeight, workRect.Bottom) - windowHeight;

			// Make sure the left-top corner of window as well.
			x = Math.Max(x, workRect.Left);
			y = Math.Max(y, workRect.Top);

			location = new Rect(x, y, windowWidth, windowHeight);
			return true;
		}
	}
}