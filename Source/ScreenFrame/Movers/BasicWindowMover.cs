using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenFrame.Movers
{
	public abstract class BasicWindowMover : WindowMover
	{
		public BasicWindowMover(Window window) : base(window)
		{ }

		private bool _isInitial = true;

		protected override void HandleWindowPosChanging(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			var position = Marshal.PtrToStructure<WindowHelper.WINDOWPOS>(lParam);

			double width = position.cx;
			double height = position.cy;

			if (_isInitial)
			{
				var dpi = VisualTreeHelperAddition.GetDpi(_window);
				width = _window.ActualWidth * dpi.DpiScaleX;
				height = _window.ActualHeight * dpi.DpiScaleY;
			}

			if ((0 < width) && (0 < height) &&
				TryGetAdjacentLocation(width, height, out Point location))
			{
				position.x = (int)location.X;
				position.y = (int)location.Y;
				position.flags &= ~WindowHelper.SWP.SWP_NOMOVE;

				Marshal.StructureToPtr<WindowHelper.WINDOWPOS>(position, lParam, true);
			}
		}

		protected override void HandleWindowPosChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			_isInitial = false;
		}

		protected abstract bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Point location);
	}
}