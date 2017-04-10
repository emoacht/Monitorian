using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenFrame.Movers
{
	public class FloatWindowMover : BasicWindowMover
	{
		private readonly Point _pivot;

		public FloatWindowMover(Window window, Point pivot) : base(window)
		{
			this._pivot = pivot;
		}

		protected override bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Point location) =>
			TryGetAdjacentLocationToPivot(windowWidth, windowHeight, out location);

		protected bool TryGetAdjacentLocationToPivot(double windowWidth, double windowHeight, out Point location)
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
					break;

				case TaskbarAlignment.Top:
					// Place this window at the bottom-left of the pivot.
					x += -windowWidth - 1;
					y += 1;
					break;

				case TaskbarAlignment.Right:
				case TaskbarAlignment.Bottom:
					// Place this window at the top-left of the pivot.
					x += -windowWidth - 1;
					y += -windowHeight - 1;
					break;

				default:
					location = default(Point);
					return false;
			}
			location = new Point(x, y);
			return true;
		}
	}
}