using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Views
{
	internal class MenuWindowMover : WindowMover
	{
		private readonly Point _pivot;

		public MenuWindowMover(Window window, Point pivot) : base(window)
		{
			this._pivot = pivot;
		}

		private bool _isInitial = true;

		protected override void HandleWindowPosChanging(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			var position = Marshal.PtrToStructure<WindowPosition.WINDOWPOS>(lParam);

			double width = position.cx;
			double height = position.cy;

			if (_isInitial)
			{
				var dpi = VisualTreeHelperAddition.GetDpi(_window);
				width = _window.ActualWidth * dpi.DpiScaleX;
				height = _window.ActualHeight * dpi.DpiScaleY;
			}

			if ((0 < width) && (0 < height))
			{
				var location = GetLocationStickToPivot(width, height);
				if (location != default(Point))
				{
					position.x = (int)location.X;
					position.y = (int)location.Y;
					position.flags &= ~WindowPosition.SWP.SWP_NOMOVE;

					Marshal.StructureToPtr<WindowPosition.WINDOWPOS>(position, lParam, true);
				}
			}
		}

		protected override void HandleWindowPosChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			_isInitial = false;
		}

		private Point GetLocationStickToPivot(double windowWidth, double windowHeight)
		{
			var x = _pivot.X;
			var y = _pivot.Y;

			var taskbarAlignment = WindowPosition.GetTaskbarAlignment();
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
					return default(Point);
			}
			return new Point(x, y);
		}
	}
}