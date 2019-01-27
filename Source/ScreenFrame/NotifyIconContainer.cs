using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ScreenFrame
{
	/// <summary>
	/// Container of NotifyIcon
	/// </summary>
	public class NotifyIconContainer : IDisposable
	{
		#region Type

		private class NotifyIconWindowListener : NativeWindow
		{
			public static NotifyIconWindowListener Create(NotifyIconContainer container)
			{
				if (!NotifyIconHelper.TryGetNotifyIconWindow(container.NotifyIcon, out NativeWindow window)
					|| (window.Handle == IntPtr.Zero))
				{
					return null;
				}
				return new NotifyIconWindowListener(container, window);
			}

			private readonly NotifyIconContainer _container;

			private NotifyIconWindowListener(NotifyIconContainer container, NativeWindow window)
			{
				this._container = container;
				this.AssignHandle(window.Handle);
			}

			protected override void WndProc(ref Message m)
			{
				_container.WndProc(ref m);

				base.WndProc(ref m);
			}

			public void Close() => this.ReleaseHandle();
		}

		#endregion

		/// <summary>
		/// NotifyIcon instance
		/// </summary>
		public NotifyIcon NotifyIcon { get; }

		private NotifyIconWindowListener _listener;

		/// <summary>
		/// Constructor
		/// </summary>
		public NotifyIconContainer()
		{
			NotifyIcon = new NotifyIcon();
			NotifyIcon.MouseClick += OnMouseClick;
			NotifyIcon.MouseDoubleClick += OnMouseDoubleClick;
		}

		/// <summary>
		/// NotifyIcon text
		/// </summary>
		public string Text
		{
			get => NotifyIcon.Text;
			set => NotifyIcon.Text = value;
		}

		#region Icon

		private System.Drawing.Icon _icon;
		private DpiScale _dpi;

		/// <summary>
		/// Shows NotifyIcon.
		/// </summary>
		/// <param name="iconPath">Path to icon for NotifyIcon</param>
		/// <param name="iconText">Text for NotifyIcon</param>
		public void ShowIcon(string iconPath, string iconText)
		{
			if (string.IsNullOrWhiteSpace(iconPath))
				throw new ArgumentNullException(nameof(iconPath));

			var iconResource = System.Windows.Application.GetResourceStream(new Uri(iconPath));
			if (iconResource != null)
			{
				using (var iconStream = iconResource.Stream)
				{
					var icon = new System.Drawing.Icon(iconStream);
					ShowIcon(icon, iconText);
				}
			}
		}

		/// <summary>
		/// Shows NotifyIcon.
		/// </summary>
		/// <param name="icon">Icon for NotifyIcon</param>
		/// <param name="iconText">Text for NotifyIcon</param>
		public void ShowIcon(System.Drawing.Icon icon, string iconText)
		{
			this._icon = icon ?? throw new ArgumentNullException(nameof(icon));
			_dpi = VisualTreeHelperAddition.GetNotificationAreaDpi();
			Text = iconText;

			NotifyIcon.Icon = GetIcon(this._icon, _dpi);
			NotifyIcon.Visible = true;

			if (_listener is null)
			{
				_listener = NotifyIconWindowListener.Create(this);
			}
		}

		private const int WM_DPICHANGED = 0x02E0;

		/// <summary>
		/// Processes windows message sent to NotifyIcon.
		/// </summary>
		/// <param name="m">Message</param>
		protected virtual void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WM_DPICHANGED:
					var oldDpi = _dpi;
					_dpi = VisualTreeHelperAddition.ConvertToDpiScale(m.WParam);
					if (!oldDpi.Equals(_dpi))
					{
						OnDpiChanged(oldDpi, _dpi);
					}
					m.Result = IntPtr.Zero;
					break;
			}
		}

		/// <summary>
		/// Called when DPI of the monitor to which NotifyIcon belongs changed.
		/// </summary>
		/// <param name="oldDpi">Old DPI information</param>
		/// <param name="newDpi">New DPI information</param>
		protected virtual void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
		{
			if (_icon != null)
			{
				NotifyIcon.Icon = GetIcon(_icon, newDpi);
			}
		}

		private static System.Drawing.Icon GetIcon(System.Drawing.Icon icon, DpiScale dpi)
		{
			var iconSize = GetIconSize(dpi);
			return new System.Drawing.Icon(icon, iconSize);
		}

		private const double Limit16 = 1.1; // Upper limit (110%) for 16x16
		private const double Limit32 = 2.0; // Upper limit (200%) for 32x32

		private static System.Drawing.Size GetIconSize(DpiScale dpi)
		{
			var factor = dpi.DpiScaleX;
			if (factor <= Limit16)
			{
				return new System.Drawing.Size(16, 16);
			}
			if (factor <= Limit32)
			{
				return new System.Drawing.Size(32, 32);
			}
			return new System.Drawing.Size(48, 48);
		}

		#endregion

		#region Click

		/// <summary>
		/// Occurs when mouse left button is clicked while mouse pointer is over NotifyIcon.
		/// </summary>
		public event EventHandler MouseLeftButtonClick;

		/// <summary>
		/// Occurs when mouse right button is clicked while mouse pointer is over NotifyIcon.
		/// </summary>
		public event EventHandler<Point> MouseRightButtonClick;

		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			NotifyIconHelper.SetNotifyIconWindowForeground(NotifyIcon);

			if (e.Button == MouseButtons.Right)
			{
				if (NotifyIconHelper.TryGetNotifyIconClickedPoint(NotifyIcon, out Point point))
					MouseRightButtonClick?.Invoke(this, point);
			}
			else
			{
				MouseLeftButtonClick?.Invoke(this, EventArgs.Empty);
			}
		}

		private void OnMouseDoubleClick(object sender, MouseEventArgs e)
		{
			MouseLeftButtonClick?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		#region IDisposable

		private bool _isDisposed = false;

		/// <summary>
		/// Public implementation of Dispose pattern
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Protected implementation of Dispose pattern
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				_listener?.Close();
				NotifyIcon.Dispose();
			}

			_isDisposed = true;
		}

		#endregion
	}
}