using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monitorian.Core.Views.Controls;

public class SwitchTextBox : TextBox
{
	public SwitchTextBox() : base()
	{
		this.PreviewMouseLeftButtonDown += (_, e) => OnDeviceDown(e.MouseDevice, true);
		this.PreviewMouseRightButtonDown += (_, e) => OnDeviceDown(e.MouseDevice, false);
		this.PreviewStylusDown += (_, e) => OnDeviceDown(e.StylusDevice, false);
		this.PreviewTouchDown += (_, e) => OnDeviceDown(e.TouchDevice, false);

		this.PreviewMouseUp += (_, _) => OnDeviceUp();
		this.PreviewStylusUp += (_, _) => OnDeviceUp();
		this.PreviewTouchUp += (_, _) => OnDeviceUp();
		this.MouseLeave += (_, _) => OnDeviceUp();
		this.StylusLeave += (_, _) => OnDeviceUp();
		this.TouchLeave += (_, _) => OnDeviceUp();

		this.IsReadOnly = true;
	}

	public TimeSpan HoldingDuration { get; set; } = TimeSpan.FromSeconds(1.2);

	private const double Tolerance = 10D;
	private InputDevice _device;
	private Point _startPosition;
	private bool _isContextMenuOpenable = true;
	private DispatcherTimer _switchTimer;

	private void OnDeviceDown(InputDevice device, bool isContextMenuOpenable)
	{
		if (!this.IsReadOnly)
			return;

		this._device = device;
		if (!TryGetDevicePosition(this._device, out _startPosition))
			return;

		this._isContextMenuOpenable = isContextMenuOpenable;

		_switchTimer ??= new DispatcherTimer(HoldingDuration, DispatcherPriority.Background, OnTick, Dispatcher.CurrentDispatcher);
		_switchTimer.Start();
	}

	private void OnDeviceUp()
	{
		_switchTimer?.Stop();

		_device = null;
	}

	private void OnTick(object sender, EventArgs e)
	{
		_switchTimer.Stop();

		if (!TryGetDevicePosition(_device, out Point endPosition))
			return;

		if (new Vector(endPosition.X - _startPosition.X, endPosition.Y - _startPosition.Y).Length > Tolerance)
			return;

		this.IsReadOnly = false;

		// Get focus.
		var scope = FocusManager.GetFocusScope(this);
		FocusManager.SetFocusedElement(scope, this);
		Keyboard.Focus(this);
		this.SelectionStart = 0;
	}

	private bool TryGetDevicePosition(InputDevice device, out Point position)
	{
		switch (device)
		{
			case MouseDevice mouse:
				position = mouse.GetPosition(this);
				return true;

			case StylusDevice stylus:
				position = stylus.GetPosition(this);
				return true;

			case TouchDevice touch:
				position = touch.GetTouchPoint(this).Position;
				return true;

			default:
				position = default;
				return false;
		}
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		// If a TextBox has focus when the window is deactivated, LostFocus event will occur
		// after Window.Deactivated event. Since a TextBox's text will be updated to source when
		// LostFocus event occurs by default, Window.Deactivated event is not always appropriate
		// to capture the latest text.

		var window = Window.GetWindow(this);
		if (!window.IsActive)
			_switchTimer?.Stop();

		this.IsReadOnly = true;

		base.OnLostFocus(e);
	}

	protected override void OnContextMenuOpening(ContextMenuEventArgs e)
	{
		if (_isContextMenuOpenable)
		{
			base.OnContextMenuOpening(e);
		}
		_isContextMenuOpenable = true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		switch (e.Key)
		{
			case Key.Escape when this.IsFocused:
				// Set focus on parent.
				var scope = FocusManager.GetFocusScope(this);
				var parent = VisualTreeHelper.GetParent(this) as UIElement;
				FocusManager.SetFocusedElement(scope, parent);
				Keyboard.Focus(parent);
				break;
		}
	}
}