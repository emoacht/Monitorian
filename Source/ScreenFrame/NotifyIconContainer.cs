using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;

namespace ScreenFrame;

/// <summary>
/// Container of <see cref="System.Windows.Forms.NotifyIcon"/>
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

	/// <summary>
	/// Encapsulates a method that has a single ref parameter and does not return a value.
	/// </summary>
	/// <typeparam name="T">The parameter of the method that this delegate encapsulates</typeparam>
	/// <param name="obj">The method that this delegate encapsulates</param>
	public delegate void RefAction<T>(ref T obj);

	#endregion

	/// <summary>
	/// NotifyIcon instance
	/// </summary>
	public NotifyIcon NotifyIcon { get; }

	private NotifyIconWindowListener _listener;

	/// <summary>
	/// NotifyIcon window handle (available only after ShowIcon method is called)
	/// </summary>
	public IntPtr NotifyIconHandle => _listener?.Handle ?? IntPtr.Zero;

	/// <summary>
	/// Windows message handlers
	/// </summary>
	/// <remarks>
	/// Key: ID number for windows message
	/// Value: Action to be called when the specified windows message is sent to NotifyIcon
	/// </remarks>
	public IDictionary<int, RefAction<Message>> Handlers { get; } = new Dictionary<int, RefAction<Message>>();

	/// <summary>
	/// Constructor
	/// </summary>
	public NotifyIconContainer()
	{
		NotifyIcon = new NotifyIcon();
		NotifyIcon.MouseClick += OnMouseClick;
		NotifyIcon.MouseDoubleClick += OnMouseDoubleClick;

		// The internal window of NotifyIcon seems to belong to the primary monitor and
		// its DPI information cannot be used as is taking into account the primary taskbar which
		// contains the notification area can be located in a monitor other than the primary one.
		Handlers[WM_DPICHANGED] = (ref Message m) =>
		{
			CheckDpiChanged();
			m.Result = IntPtr.Zero;
		};
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
	public void ShowIcon(string iconPath) => ShowIcon(iconPath, Text);

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
		if (iconResource is not null)
		{
			using var iconStream = iconResource.Stream;
			var icon = new System.Drawing.Icon(iconStream);
			ShowIcon(icon, iconText);
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

		_listener ??= NotifyIconWindowListener.Create(this);
	}

	/// <summary>
	/// Gets the rectangle of NotifyIcon.
	/// </summary>
	/// <returns>Rectangle of NotifyIcon</returns>
	public Rect GetIconRect()
	{
		NotifyIconHelper.TryGetNotifyIconRect(NotifyIcon, out Rect iconRect);
		return iconRect;
	}

	/// <summary>
	/// Processes windows messages sent to NotifyIcon.
	/// </summary>
	/// <param name="m">Windows message</param>
	protected virtual void WndProc(ref Message m)
	{
		if (Handlers.TryGetValue(m.Msg, out RefAction<Message> action))
			action.Invoke(ref m);
	}

	private const int WM_DPICHANGED = 0x02E0;

	private void HandleDpiChanged(ref Message m)
	{
		var oldDpi = _dpi;
		_dpi = VisualTreeHelperAddition.ConvertToDpiScale(m.WParam);
		if (!oldDpi.Equals(_dpi))
		{
			OnDpiChanged(oldDpi, _dpi);
		}
		m.Result = IntPtr.Zero;
	}

	private void CheckDpiChanged()
	{
		if (!NotifyIconHelper.TryGetNotifyIconRect(NotifyIcon, out Rect iconRect))
			return;

		var oldDpi = _dpi;
		_dpi = VisualTreeHelperAddition.GetDpi(iconRect.Location);
		if (!oldDpi.Equals(_dpi))
		{
			OnDpiChanged(oldDpi, _dpi);
		}
	}

	/// <summary>
	/// Called when DPI of the monitor to which NotifyIcon belongs changed.
	/// </summary>
	/// <param name="oldDpi">Old DPI information</param>
	/// <param name="newDpi">New DPI information</param>
	protected virtual void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
	{
		if (_icon is not null)
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
	private const double Limit32 = 1.5; // Upper limit (150%) for 32x32
	private const double Limit40 = 2.0; // Upper limit (200%) for 40x40

	private static System.Drawing.Size GetIconSize(DpiScale dpi)
	{
		return dpi.DpiScaleX switch
		{
			<= Limit16 => new System.Drawing.Size(16, 16),
			<= Limit32 => new System.Drawing.Size(32, 32),
			<= Limit40 => new System.Drawing.Size(40, 40),
			_ => new System.Drawing.Size(48, 48)
		};
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

		switch (e.Button)
		{
			case MouseButtons.Left:
				MouseLeftButtonClick?.Invoke(sender, EventArgs.Empty);
				break;

			case MouseButtons.Right:
				if (NotifyIconHelper.TryGetNotifyIconCursorLocation(NotifyIcon, out Point location, isSubstitutable: true))
					MouseRightButtonClick?.Invoke(sender, location);

				break;
		}

		CheckDpiChanged();
	}

	private void OnMouseDoubleClick(object sender, MouseEventArgs e)
	{
		MouseLeftButtonClick?.Invoke(sender, EventArgs.Empty);

		CheckDpiChanged();
	}

	#endregion

	#region Hover

	private readonly object _lock = new();

	/// <summary>
	/// Occurs when mouse wheel is rotated while mouse pointer is over NotifyIcon.
	/// </summary>
	public event EventHandler<int> MouseWheel
	{
		add
		{
			lock (_lock)
			{
				RegisterMouseMove();
				_mouseWheel += value;
			}
		}
		remove
		{
			lock (_lock)
			{
				_mouseWheel -= value;
				UnregisterMouseMove();
			}
		}
	}
	private event EventHandler<int> _mouseWheel;

	private void RegisterMouseMove()
	{
		if (_mouseWheel is null)
		{
			NotifyIcon.MouseMove += OnMouseMove;
		}
	}

	private void UnregisterMouseMove()
	{
		if (_mouseWheel is null)
		{
			NotifyIcon.MouseMove -= OnMouseMove;
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		ShowOverlayWindow();
	}

	private Window _window;

	private void ShowOverlayWindow()
	{
		if (_window?.Visibility is Visibility.Visible)
			return;

		if (_window is null)
		{
			// Create a semi-transparent topmost Window.
			_window = new Window
			{
				WindowStyle = WindowStyle.None,
				AllowsTransparency = true,
				Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 0, 0, 0)),
				Topmost = true,
				ShowActivated = false,
				ShowInTaskbar = false,
				Width = 1,
				Height = 1,
			};

			_window.MouseUp += OnWindowMouseUp;
			_window.MouseDoubleClick += OnWindowMouseDoubleClick;
			_window.MouseWheel += OnWindowMouseWheel;
			_window.MouseLeave += OnWindowMouseLeave;
		}

		if (NotifyIconHelper.TryGetNotifyIconRect(NotifyIcon, out Rect iconRect))
		{
			_window.ToolTip = Text;

			if (WindowHelper.TryGetTaskbar(out _, out TaskbarAlignment taskbarAlignment))
			{
				var placement = taskbarAlignment switch
				{
					TaskbarAlignment.Top => System.Windows.Controls.Primitives.PlacementMode.Bottom,
					_ => System.Windows.Controls.Primitives.PlacementMode.Top
				};
				System.Windows.Controls.ToolTipService.SetPlacement(_window, placement);
			}

			_window.Visibility = Visibility.Visible;
			_window.Show();
			WindowHelper.SetWindowPosition(_window, iconRect, false);
		}

		void OnWindowMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var button = e.ChangedButton switch
			{
				System.Windows.Input.MouseButton.Left => MouseButtons.Left,
				System.Windows.Input.MouseButton.Middle => MouseButtons.Middle,
				System.Windows.Input.MouseButton.Right => MouseButtons.Right,
				_ => default
			};
			OnMouseClick(sender, new MouseEventArgs(button, e.ClickCount, 0, 0, 0));
		}

		void OnWindowMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			OnMouseDoubleClick(sender, new MouseEventArgs(MouseButtons.Left, e.ClickCount, 0, 0, 0));
		}

		void OnWindowMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			_mouseWheel?.Invoke(sender, e.Delta);
		}

		void OnWindowMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			_window.Visibility = Visibility.Collapsed;

			// Changing WindowState of a Window which is Topmost will deactivate the app.
			//_window.WindowState = WindowState.Minimized;
		}
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
			_window?.Close();
		}

		_isDisposed = true;
	}

	#endregion
}