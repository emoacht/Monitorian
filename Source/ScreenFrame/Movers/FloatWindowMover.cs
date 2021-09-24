using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using ScreenFrame.Helper;

namespace ScreenFrame.Movers
{
	/// <summary>
	/// Mover of <see cref="System.Windows.Window"/> which implements functions for float window
	/// </summary>
	public class FloatWindowMover : BasicWindowMover
	{
		private readonly Point _pivot;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="window">Window to be moved</param>
		/// <param name="pivot">Pivot point to be referred</param>
		public FloatWindowMover(Window window, Point pivot) : base(window)
		{
			this._pivot = pivot;
		}

		/// <summary>
		/// Alignment of pivot
		/// </summary>
		public override PivotAlignment PivotAlignment { get; protected set; }

		/// <summary>
		/// Gets Per-Monitor DPI of the monitor.
		/// </summary>
		/// <returns>DPI information</returns>
		protected override DpiScale GetDpi() => VisualTreeHelperAddition.GetDpi(_pivot);

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
			if (!WindowHelper.TryGetTaskbar(out _, out TaskbarAlignment taskbarAlignment, out _))
			{
				location = default;
				return false;
			}

			var isLeftToRight = !CultureInfoAddition.UserDefaultUICulture.TextInfo.IsRightToLeft;

			PivotAlignment = (taskbarAlignment, isLeftToRight) switch
			{
				(TaskbarAlignment.Top, true) => PivotAlignment.TopRight,
				(TaskbarAlignment.Top, false) => PivotAlignment.TopLeft,
				(TaskbarAlignment.Bottom, true) => PivotAlignment.BottomRight,
				(TaskbarAlignment.Bottom, false) => PivotAlignment.BottomLeft,
				(TaskbarAlignment.Left, { }) => PivotAlignment.BottomLeft,
				(TaskbarAlignment.Right, { }) => PivotAlignment.BottomRight,
				_ => default
			};

			var x = _pivot.X;
			var y = _pivot.Y;

			switch (PivotAlignment)
			{
				case PivotAlignment.TopLeft:
					x += 1;
					y += 1;
					break;
				case PivotAlignment.TopRight:
					x -= (windowWidth + 1);
					y += 1;
					break;
				case PivotAlignment.BottomLeft:
					x += 1;
					y -= (windowHeight + 1);
					break;
				case PivotAlignment.BottomRight:
					x -= (windowWidth + 1);
					y -= (windowHeight + 1);
					break;
			}
			location = new Rect(x, y, windowWidth, windowHeight);
			return true;
		}
	}
}