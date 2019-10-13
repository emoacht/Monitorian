using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
			var x = _pivot.X;
			var y = _pivot.Y;

			var taskbarAlignment = WindowHelper.GetTaskbarAlignment();
			switch (taskbarAlignment)
			{
				case TaskbarAlignment.Left:
					// Place this window at the top-right of the pivot.
					x += 1;
					y += -windowHeight - 1;
					PivotAlignment = PivotAlignment.BottomLeft;
					break;

				case TaskbarAlignment.Top:
					// Place this window at the bottom-left of the pivot.					
					x += -windowWidth - 1;
					y += 1;
					PivotAlignment = PivotAlignment.TopRight;
					break;

				case TaskbarAlignment.Right:
				case TaskbarAlignment.Bottom:
					// Place this window at the top-left of the pivot.
					x += -windowWidth - 1;
					y += -windowHeight - 1;
					PivotAlignment = PivotAlignment.BottomRight;
					break;

				default:
					location = default;
					return false;
			}
			location = new Rect(x, y, windowWidth, windowHeight);
			return true;
		}
	}
}