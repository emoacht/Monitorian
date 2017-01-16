using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Monitorian.Views.Movers
{
	internal class MainWindowMover : WindowMover
	{
		private readonly NotifyIcon _notifyIcon;

		public MainWindowMover(Window window, NotifyIcon notifyIcon) : base(window)
		{
			if (notifyIcon == null)
				throw new ArgumentNullException(nameof(notifyIcon));

			this._notifyIcon = notifyIcon;
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
				var location = GetLocationStickToTaskbar(width, height);
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

		private Point GetLocationStickToTaskbar(double windowWidth, double windowHeight)
		{
			Rect taskbarRect;
			TaskbarAlignment taskbarAlignment;
			if (!WindowPosition.TryGetTaskbar(out taskbarRect, out taskbarAlignment))
				return default(Point);

			var iconRect = WindowPosition.GetNotifyIconRect(_notifyIcon);
			if (iconRect == Rect.Empty)
				iconRect = taskbarRect; // Fallback

			var margin = WindowPosition.GetDwmWindowMargin(_window);
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
			return new Point(x, y);
		}
	}
}