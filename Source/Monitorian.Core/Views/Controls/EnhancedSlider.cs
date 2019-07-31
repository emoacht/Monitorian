using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Monitorian.Core.Views.Controls
{
	public class EnhancedSlider : Slider
	{
		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			this.IsSnapToTickEnabled = true;
		}

		private Track _track;
		private Thumb _thumb;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_track = this.GetTemplateChild("PART_Track") as Track;
			_thumb = _track?.Thumb;
			CheckCanDrag();
		}

		#region Drag

		protected bool CanDrag { get; private set; }
		protected bool IsDragging => (_thumb?.IsDragging == true);

		private void CheckCanDrag()
		{
			CanDrag = (_thumb != null) && FindNonPublicMembers();
			if (!CanDrag)
			{
				// Fallback
				this.IsMoveToPointEnabled = true;
				this.IsManipulationEnabled = true;
			}
		}

		private static MethodInfo _updateValue;
		private static PropertyInfo _thumbIsDragging;
		private static FieldInfo _thumbOriginThumbPoint;
		private static FieldInfo _thumbPreviousScreenCoordPosition;
		private static FieldInfo _thumbOriginScreenCoordPosition;

		private static bool FindNonPublicMembers()
		{
			// Slider.UpdateValue private method
			_updateValue = typeof(Slider).GetMethod("UpdateValue", BindingFlags.NonPublic | BindingFlags.Instance);

			// Thumb.IsDragging public readonly property
			_thumbIsDragging = typeof(Thumb).GetProperty("IsDragging", BindingFlags.Public | BindingFlags.Instance);

			// Thumb._originThumbPoint private field
			_thumbOriginThumbPoint = typeof(Thumb).GetField("_originThumbPoint", BindingFlags.NonPublic | BindingFlags.Instance);

			// Thumb._previousScreenCoordPosition private field
			_thumbPreviousScreenCoordPosition = typeof(Thumb).GetField("_previousScreenCoordPosition", BindingFlags.NonPublic | BindingFlags.Instance);

			// Thumb._originScreenCoordPosition private field
			_thumbOriginScreenCoordPosition = typeof(Thumb).GetField("_originScreenCoordPosition", BindingFlags.NonPublic | BindingFlags.Instance);

			return (_updateValue != null)
				&& (_thumbIsDragging != null)
				&& (_thumbOriginThumbPoint != null)
				&& (_thumbPreviousScreenCoordPosition != null)
				&& (_thumbOriginScreenCoordPosition != null);
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

		// This method will not be called when the event is handled by OnPreviewStylusDown method.
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

		private bool SetValueStartDrag(
			Func<IInputElement, Point> getPosition,
			Action<UIElement> captureDevice)
		{
			if (!CanDrag)
				return false;

			if (!this.IsFocused)
				this.Focus();

			var originTrackPoint = getPosition(_track);
			var newValue = _track.ValueFromPoint(originTrackPoint);
			newValue = Math.Min(this.Maximum, Math.Max(this.Minimum, Math.Round(newValue)));

			if (newValue == this.Value)
				return false;

			// Set new value.
			_updateValue.Invoke(this, new object[] { newValue });

			// Reproduce Thumb.OnMouseLeftButtonDown method.
			if (!_thumb.IsDragging)
			{
				// Start drag operation for Thumb.
				_thumb.Focus();
				captureDevice(_thumb);

				_thumbIsDragging.SetValue(_thumb, true);

				var originThumbPoint = getPosition(_thumb);
				var originThumbPointToScreen = _thumb.PointToScreen(originThumbPoint);
				//_thumbOriginThumbPoint.SetValue(_thumb, originThumbPoint);
				_thumbPreviousScreenCoordPosition.SetValue(_thumb, originThumbPointToScreen);
				_thumbOriginScreenCoordPosition.SetValue(_thumb, originThumbPointToScreen);

				try
				{
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

		protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
		{
			base.OnManipulationCompleted(e);
		}

		protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
		{
			base.OnManipulationDelta(e);

			var cumulativeDistance = e.CumulativeManipulation.Translation;
			var cumulativeValue = _track.ValueFromDistance(cumulativeDistance.X, cumulativeDistance.Y);
			var newValue = _originValue + cumulativeValue;
			newValue = Math.Min(this.Maximum, Math.Max(this.Minimum, Math.Round(newValue)));

			if (this.Value == newValue)
				return;

			this.Value = newValue;
		}

		#endregion

		#region MouseWheel

		private const double ReductionFactor = 0.05;

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			if (!this.IsFocused)
				this.Focus();

			if (e.Delta == 0)
				return;

			var newValue = this.Value + e.Delta * ReductionFactor;
			newValue = Math.Min(this.Maximum, Math.Max(this.Minimum, Math.Round(newValue)));

			this.Value = newValue;
		}

		#endregion
	}
}