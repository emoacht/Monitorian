using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

using Monitorian.Core.Views.Input;
using Monitorian.Core.Views.Input.Touchpad;

namespace Monitorian.Core.Views.Controls
{
	public class EnhancedSlider : Slider
	{
		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			this.IsSnapToTickEnabled = false;
			this.AutoToolTipPlacement = AutoToolTipPlacement.TopLeft;
		}

		private Track _track;
		private Thumb _thumb;

		private TouchpadTracker _tracker;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_track = this.GetTemplateChild("PART_Track") as Track;
			_thumb = _track?.Thumb;
			CheckCanDrag();

			_tracker = TouchpadTracker.Create(this);
		}

		public bool ChangeValue(double changeSize)
		{
			return UpdateValue(this.Value + changeSize);
		}

		protected virtual bool UpdateValue(double value)
		{
			// Slider.SnapToTick property will not be reflected like Slider.UpdateValue method.
			// Still, the value will snap to each integer.

			var adjustedValue = Math.Min(this.Maximum, Math.Max(this.Minimum, Math.Round(value)));
			if (this.Value == adjustedValue)
				return false;

			this.Value = adjustedValue;
			return true;
		}

		#region Drag

		protected bool CanDrag { get; private set; }
		protected bool IsDragging => (_thumb is { IsDragging: true });

		private void CheckCanDrag()
		{
			CanDrag = (_thumb is not null) && (_canAccessNonPublicMembers ??= FindNonPublicMembers());
			if (!CanDrag)
			{
				// Fallback
				this.IsMoveToPointEnabled = true;
				this.IsManipulationEnabled = true;
			}
		}

		private static bool? _canAccessNonPublicMembers;

		private static PropertyInfo _thumbIsDragging;
		private static FieldInfo _thumbOriginThumbPoint;
		private static FieldInfo _thumbPreviousScreenCoordPosition;
		private static FieldInfo _thumbOriginScreenCoordPosition;
		private static MethodInfo _reposition;

		private static bool FindNonPublicMembers()
		{
			// Thumb.IsDragging public readonly property
			_thumbIsDragging = typeof(Thumb).GetProperty("IsDragging", BindingFlags.Public | BindingFlags.Instance);

			// Thumb._originThumbPoint private field
			_thumbOriginThumbPoint = typeof(Thumb).GetField("_originThumbPoint", BindingFlags.NonPublic | BindingFlags.Instance);

			// Thumb._previousScreenCoordPosition private field
			_thumbPreviousScreenCoordPosition = typeof(Thumb).GetField("_previousScreenCoordPosition", BindingFlags.NonPublic | BindingFlags.Instance);

			// Thumb._originScreenCoordPosition private field
			_thumbOriginScreenCoordPosition = typeof(Thumb).GetField("_originScreenCoordPosition", BindingFlags.NonPublic | BindingFlags.Instance);

			// Popup.Reposition internal method
			_reposition = typeof(Popup).GetMethod("Reposition", BindingFlags.NonPublic | BindingFlags.Instance);

			return (_thumbIsDragging is not null)
				&& (_thumbOriginThumbPoint is not null)
				&& (_thumbPreviousScreenCoordPosition is not null)
				&& (_thumbOriginScreenCoordPosition is not null)
				&& (_reposition is not null);
		}

		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			if (SetValueStartDrag(
				x => e.GetPosition(x),
				x => x.CaptureMouse()))
			{
				e.Handled = true;
			}

			base.OnPreviewMouseDown(e);
		}

		// OnPreviewMouseUp covers the case of OnPreviewStylusUp or OnPreviewTouchUp.
		protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseUp(e);

			EnsureUpdateSource();
		}

		// OnPreviewStylusDown covers the case of OnPreviewTouchDown.
		protected override void OnPreviewStylusDown(StylusDownEventArgs e)
		{
			if (SetValueStartDrag(
				x => e.GetPosition(x),
				x => x.CaptureStylus()))
			{
				e.Handled = true;
			}

			base.OnPreviewStylusDown(e);
		}

		protected override void OnPreviewStylusUp(StylusEventArgs e)
		{
			base.OnPreviewStylusUp(e);

			// ReleaseStylusCapture method will release Touch capture as well.
			this.ReleaseStylusCapture();
			VisualStateManager.GoToState(_thumb, "Normal", true);
		}

		protected override void OnPreviewTouchDown(TouchEventArgs e)
		{
			if (SetValueStartDrag(
				x => e.GetTouchPoint(x).Position,
				x => x.CaptureTouch(e.TouchDevice)))
			{
				e.Handled = true;
			}

			base.OnPreviewTouchDown(e);
		}

		protected virtual bool SetValueStartDrag(
			Func<IInputElement, Point> getPosition,
			Action<UIElement> captureDevice)
		{
			if (!CanDrag)
				return false;

			if (!this.IsFocused)
				this.Focus();

			var originTrackPoint = getPosition(_track);
			var newValue = _track.ValueFromPoint(originTrackPoint);
			if (!UpdateValue(newValue))
				return false;

			// Reproduce Thumb.OnMouseLeftButtonDown method.
			if (!_thumb.IsDragging)
			{
				// Start drag operation for Thumb.
				_thumb.Focus();
				captureDevice(_thumb);

				_thumbIsDragging.SetValue(_thumb, true);

				var originThumbPoint = getPosition(_thumb);
				var originThumbPointToScreen = _thumb.PointToScreen(originThumbPoint);
				_thumbOriginThumbPoint.SetValue(_thumb, originThumbPoint);
				_thumbPreviousScreenCoordPosition.SetValue(_thumb, originThumbPointToScreen);
				_thumbOriginScreenCoordPosition.SetValue(_thumb, originThumbPointToScreen);

				try
				{
					// Trigger Thumb.DragStartedEvent event.
					_thumb.RaiseEvent(new DragStartedEventArgs(originThumbPoint.X, originThumbPoint.Y));
				}
				catch
				{
					_thumb.CancelDrag();
					throw;
				}
			}
			return true;
		}

		protected override void OnThumbDragStarted(DragStartedEventArgs e)
		{
			OpenToolTip(_track);

			e.Handled = true; // This is necessary to prevent another ToolTip from being shown.
		}

		protected override void OnThumbDragDelta(DragDeltaEventArgs e)
		{
			var newValue = _track.Value + _track.ValueFromDistance(e.HorizontalChange, e.VerticalChange);
			UpdateValue(newValue);

			OpenToolTip(_track);
		}

		protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
		{
			CloseToolTip(_track);
		}

		private ToolTip _autoToolTip;

		protected virtual void OpenToolTip(Track track)
		{
			if (!_canAccessNonPublicMembers.GetValueOrDefault())
				return;

			_autoToolTip ??= new ToolTip
			{
				Placement = PlacementMode.Custom,
				CustomPopupPlacementCallback = new CustomPopupPlacementCallback(AutoToolTipCustomPlacementCallback)
			};

			if (track.Thumb.ToolTip != _autoToolTip)
			{
				track.Thumb.ToolTip = _autoToolTip;
				_autoToolTip.PlacementTarget = track.Thumb;
			}

			_autoToolTip.Content = track.Value.ToString("F0");
			_autoToolTip.IsOpen = true;

			_reposition.Invoke((Popup)_autoToolTip.Parent, null);

			CustomPopupPlacement[] AutoToolTipCustomPlacementCallback(Size popupSize, Size targetSize, Point offset)
			{
				// Accept the combination of AutoToolTipPlacement.TopLeft and Orientation.Horizontal only.
				switch (this.AutoToolTipPlacement, this.Orientation)
				{
					case (AutoToolTipPlacement.TopLeft, Orientation.Horizontal):
						// Place popup at top of thumb
						return new[] { new CustomPopupPlacement(new Point((targetSize.Width - popupSize.Width) * 0.5, -popupSize.Height), PopupPrimaryAxis.Horizontal) };
					default:
						throw new InvalidOperationException();
				}
			}
		}

		protected virtual void CloseToolTip(Track track)
		{
			if (_autoToolTip is not null)
				_autoToolTip.IsOpen = false;

			track.Thumb.ToolTip = null;
		}

		#endregion

		#region Manipulation

		private double _originValue;

		protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
		{
			base.OnManipulationStarted(e);

			if (!this.IsFocused)
				this.Focus();

			_originValue = _track.ValueFromPoint(e.ManipulationOrigin);
		}

		protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
		{
			base.OnManipulationDelta(e);

			var cumulativeDistance = e.CumulativeManipulation.Translation;
			var cumulativeValue = _track.ValueFromDistance(cumulativeDistance.X, cumulativeDistance.Y);
			var newValue = _originValue + cumulativeValue;
			UpdateValue(newValue);
		}

		protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
		{
			base.OnManipulationCompleted(e);

			EnsureUpdateSource();
		}

		#endregion

		#region MouseWheel

		public static int WheelFactor { get; set; } = 5;

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			if (!this.IsFocused)
				this.Focus();

			if (e.Delta is 0)
				return;

			bool IsTouchpad() => (e.Timestamp - _tracker.LastInputTimeStamp <= 500);

			int delta = e.Delta;
			if ((e.RoutedEvent == Mouse.MouseWheelEvent && IsTouchpad()) ||
				(e.RoutedEvent == MouseAddition.MouseHorizontalWheelEvent))
			{
				delta *= -1;
			}

			// The default wheel rotation delta (for one notch) is set at 120.
			// This value is seen as WHEEL_DELTA and System.Windows.Input.Mouse.MouseWheelDeltaForOneLine.
			// Although a wheel rotation delta is expressed in multiples or divisions of WHEEL_DELTA,
			// usually, it will be a multiple of WHEEL_DELTA (+120/-120).
			// Some explanations are provided in:
			// WM_MOUSEWHEEL Message
			// https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousewheel
			// System.Windows.Forms.MouseEventArgs.Delta Property
			// https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.mouseeventargs.delta
			//
			// Mouse.MouseWheelDeltaForOneLine should be casted to double in case the delta is smaller than 120.
			var newValue = this.Value + (delta / (double)Mouse.MouseWheelDeltaForOneLine * WheelFactor);
			UpdateValue(newValue);
			EnsureUpdateSource();
		}

		#endregion

		#region Deferral

		public bool DefersUpdateSource
		{
			get { return (bool)GetValue(DefersUpdateSourceProperty); }
			set { SetValue(DefersUpdateSourceProperty, value); }
		}
		public static readonly DependencyProperty DefersUpdateSourceProperty =
			DependencyProperty.Register(
				"DefersUpdateSource",
				typeof(bool),
				typeof(EnhancedSlider),
				new PropertyMetadata(
					false,
					(d, e) => ((EnhancedSlider)d).PrepareUpdateSource((bool)e.NewValue)));

		private BindingExpression _valuePropertyExpression;

		protected virtual void PrepareUpdateSource(bool defer)
		{
			if (defer)
			{
				_valuePropertyExpression = ReplaceBinding(this, ValueProperty, BindingMode.TwoWay, UpdateSourceTrigger.Explicit, 0);
			}
			else if (_valuePropertyExpression is not null)
			{
				ReplaceBinding(this, ValueProperty, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged, 50);
				_valuePropertyExpression = null;
			}

			static BindingExpression ReplaceBinding(DependencyObject target, DependencyProperty dp, BindingMode mode, UpdateSourceTrigger trigger, int delay)
			{
				var bindingPath = BindingOperations.GetBinding(target, dp)?.Path.Path;
				if (string.IsNullOrEmpty(bindingPath))
					return null;

				BindingOperations.ClearBinding(target, dp); // This does not work if the binding is set in DataTemplate.

				var binding = new Binding(bindingPath)
				{
					Mode = mode,
					UpdateSourceTrigger = trigger,
					Delay = delay
				};
				return BindingOperations.SetBinding(target, dp, binding) as BindingExpression;
			}
		}

		public virtual void EnsureUpdateSource()
		{
			_valuePropertyExpression?.UpdateSource();
		}

		#endregion
	}
}