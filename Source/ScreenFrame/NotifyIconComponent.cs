using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ScreenFrame
{
	public class NotifyIconComponent : Component
	{
		private readonly Container _container;

		public NotifyIcon NotifyIcon { get; }

		public NotifyIconComponent()
		{
			_container = new Container();

			NotifyIcon = new NotifyIcon(_container) { ContextMenuStrip = new ContextMenuStrip() };
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
		}

		public void AdjustIcon(DpiScale dpi)
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
			if (e.Button == MouseButtons.Right)
			{
				MouseRightButtonClick?.Invoke(this, GetNotifyIconClickedPoint());
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

		/// <summary>
		/// Gets the point where NotifyIcon is clicked from the position of ContextMenuStrip and NotifyIcon.
		/// </summary>
		/// <returns>Clicked point</returns>
		/// <remarks>MouseEventArgs.Location property of MouseClick event does not contain data.</remarks>
		private Point GetNotifyIconClickedPoint()
		{
			var contextMenuStrip = NotifyIcon.ContextMenuStrip;

			var corners = new Point[]
			{
				//new Point(contextMenuStrip.Left, contextMenuStrip.Top),
				//new Point(contextMenuStrip.Right, contextMenuStrip.Top),
				new Point(contextMenuStrip.Left, contextMenuStrip.Bottom),
				new Point(contextMenuStrip.Right, contextMenuStrip.Bottom)
			};

			if (WindowPosition.TryGetNotifyIconRect(NotifyIcon, out Rect iconRect))
			{
				foreach (var corner in corners)
				{
					if (iconRect.Contains(corner))
						return corner;
				}
			}
			return corners.Last(); // Fallback
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
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		#endregion
	}
}