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
	/// Mover of <see cref="System.Windows.Window"/> which implements basic functions
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
		/// Alignment of pivot
		/// </summary>
		public abstract PivotAlignment PivotAlignment { get; protected set; }

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
				TryGetAdjacentLocation(width, height, out Rect adjacentLocation) &&
				TryConfirmLocation(ref adjacentLocation))
			{
				position.x = (int)adjacentLocation.X;
				position.y = (int)adjacentLocation.Y;
				position.flags &= ~WindowHelper.SWP.SWP_NOMOVE;

				if (((int)adjacentLocation.Width < (int)width) || ((int)adjacentLocation.Height < (int)height))
				{
					position.cx = (int)adjacentLocation.Width;
					position.cy = (int)adjacentLocation.Height;
					position.flags &= ~WindowHelper.SWP.SWP_NOSIZE;
				}

				Marshal.StructureToPtr<WindowHelper.WINDOWPOS>(position, lParam, true);
			}
		}

		/// <summary>
		/// Handles window position changed event.
		/// </summary>
		protected override void HandleWindowPosChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{ }

		/// <summary>
		/// Handles Display change event.
		/// </summary>
		protected override void HandleDisplayChange(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{ }

		/// <summary>
		/// Attempts to get the adjacent location using specified window width and height.
		/// </summary>
		/// <param name="windowWidth">Window width</param>
		/// <param name="windowHeight">Window height</param>
		/// <param name="location">Location of window</param>
		/// <returns>True if successfully gets</returns>
		protected abstract bool TryGetAdjacentLocation(double windowWidth, double windowHeight, out Rect location);

		/// <summary>
		/// Attempts to confirm that a specified location is not completely outside of monitors.
		/// </summary>
		/// <param name="location">Location of window</param>
		/// <returns>True if successfully confirms</returns>
		/// <remarks>
		/// The specified location and the current location are not necessarily in the same monitor.
		/// </remarks>
		protected virtual bool TryConfirmLocation(ref Rect location)
		{
			if (!WindowHelper.TryGetMonitorRect(location, out Rect monitorRect, out _))
				return false;

			if (monitorRect.Contains(location))
				return true;

			// Rect.IntersectsWith method is not enough to check intersection because it will return
			// true when two rectangles share only the outline.
			// If intersection is Rect.Empty, its width and height will be double.NegativeInfinity.	
			if (Rect.Intersect(location, monitorRect).IsEmpty)
				return false;

			var left = Math.Max(monitorRect.Left, Math.Min(monitorRect.Right, location.Right) - location.Width);
			var top = Math.Max(monitorRect.Top, Math.Min(monitorRect.Bottom, location.Bottom) - location.Height);
			location = new Rect(left, top, location.Width, location.Height);
			return true;
		}
	}
}