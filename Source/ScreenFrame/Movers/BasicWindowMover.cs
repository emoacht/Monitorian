using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenFrame.Movers
{
	/// <summary>
	/// Window mover which implements basic functions
	/// </summary>
	public abstract class BasicWindowMover : WindowMover
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="window">Window to be moved</param>
		public BasicWindowMover(Window window) : base(window)
		{ }

		/// <summary>
		/// Handles window position changing event.
		/// </summary>
		protected override void HandleWindowPosChanging(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			var position = Marshal.PtrToStructure<WindowHelper.WINDOWPOS>(lParam);

			double width = position.cx;
			double height = position.cy;

			if (position.flags.HasFlag(WindowHelper.SWP.SWP_SHOWWINDOW))
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

		/// <summary>
		/// Handles window position changed event.
		/// </summary>
		protected override void HandleWindowPosChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{ }

		/// <summary>
		/// Tries to get the adjacent location using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if succeeded</returns>
		protected abstract bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Point location);
	}
}