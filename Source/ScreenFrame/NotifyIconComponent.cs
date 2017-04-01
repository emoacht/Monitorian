using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using ScreenFrame.Helper;

namespace ScreenFrame
{
	public class NotifyIconComponent : Component
	{
		#region Type

		private class NotifyIconWindowListener : NativeWindow
		{
			public static NotifyIconWindowListener Create(NotifyIcon notifyIcon)
			{
				if (!NotifyIconHelper.TryGetNotifyIconWindow(notifyIcon, out NativeWindow window) ||
					(window.Handle == IntPtr.Zero))
				{
					return null;
				}
				return new NotifyIconWindowListener(window);
			}

			private NotifyIconWindowListener(NativeWindow window) => this.AssignHandle(window.Handle);

			public event EventHandler<Message> OnWindowMessageReceived;

			protected override void WndProc(ref Message m)
			{
				OnWindowMessageReceived?.Invoke(this, m);

				base.WndProc(ref m);
			}

			public void Close()
			{
				OnWindowMessageReceived = null;
				this.ReleaseHandle();
			}
		}

		#endregion

		private readonly Container _container;
		private NotifyIconWindowListener _listener;

		public NotifyIcon NotifyIcon { get; }

		public NotifyIconComponent()
		{
			_container = new Container();

			NotifyIcon = new NotifyIcon(_container);
			NotifyIcon.MouseClick += OnMouseClick;
			NotifyIcon.MouseDoubleClick += OnMouseDoubleClick;
		}

		public string Text
		{
			get { return NotifyIcon.Text; }
			set { NotifyIcon.Text = value; }
		}

		#region Icon

		private System.Drawing.Icon _icon;
		public DpiScale _dpi;

		public void ShowIcon(string iconPath, string iconText) =>
			ShowIcon(iconPath, iconText, VisualTreeHelperAddition.GetNotificationAreaDpi());

		public void ShowIcon(string iconPath, string iconText, DpiScale dpi)
		{
			if (string.IsNullOrWhiteSpace(iconPath))
				throw new ArgumentNullException(nameof(iconPath));

			var iconResource = System.Windows.Application.GetResourceStream(new Uri(iconPath));
			if (iconResource != null)
			{
				using (var iconStream = iconResource.Stream)
				{
					var icon = new System.Drawing.Icon(iconStream);
					ShowIcon(icon, iconText, dpi);
				}
			}
		}

		public void ShowIcon(System.Drawing.Icon icon, string iconText, DpiScale dpi)
		{
			this._icon = icon ?? throw new ArgumentNullException(nameof(icon));
			this._dpi = dpi;
			this.Text = iconText;

			NotifyIcon.Icon = GetIcon(this._icon, this._dpi);
			NotifyIcon.Visible = true;

			if (_listener == null)
			{
				_listener = NotifyIconWindowListener.Create(NotifyIcon);
				if (_listener != null)
				{
					_listener.OnWindowMessageReceived += OnNotifyIconWindowMessageReceived;
				}
			}
		}

		private const int WM_DPICHANGED = 0x02E0;

		private void OnNotifyIconWindowMessageReceived(object sender, Message m)
		{
			switch (m.Msg)
			{
				case WM_DPICHANGED:
					var dpi = DpiScaleExtension.FromUInt((uint)m.WParam);
					AdjustIcon(dpi);
					break;
			}
		}

		private void AdjustIcon(DpiScale dpi)
		{
			if (_icon == null)
				return;

			if (dpi.Equals(this._dpi))
				return;

			this._dpi = dpi;
			NotifyIcon.Icon = GetIcon(_icon, this._dpi);
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

		public event EventHandler MouseLeftButtonClick;
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
				MouseLeftButtonClick?.Invoke(this, null);
			}
		}

		private void OnMouseDoubleClick(object sender, MouseEventArgs e)
		{
			MouseLeftButtonClick?.Invoke(this, null);
		}

		#endregion

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				_container.Dispose();
				_listener?.Close();
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		#endregion
	}
}