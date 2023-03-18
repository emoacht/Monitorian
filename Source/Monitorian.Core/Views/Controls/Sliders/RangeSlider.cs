using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Monitorian.Core.Views.Controls
{
	[TemplatePart(Name = nameof(Named.PART_StartTrack), Type = typeof(Track))]
	[TemplatePart(Name = nameof(Named.PART_EndTrack), Type = typeof(Track))]
	public class RangeSlider : EnhancedSlider
	{
		static RangeSlider()
		{
			OverrideMetadata();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			this.Minimum = 0;
			this.Maximum = 100;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			FindTemplateMembers();
		}

		#region Range

		/// <summary>
		/// Attempts to get the current level (from 0 to 1) within selected range. 
		/// </summary>
		/// <param name="level">Current level</param>
		/// <returns>True if the current value is within selected range</returns>
		/// <remarks>
		/// This level is translated from/to the current value.
		/// </remarks>
		protected virtual bool TryGetLevel(out double level) => TryGetLevel(this.Value, out level);

		protected virtual bool TryGetLevel(double value, out double level)
		{
			if (value < this.SelectionStart)
			{
				level = 0;
				return false;
			}
			if (value > this.SelectionEnd)
			{
				level = 1;
				return false;
			}
			level = (value - this.SelectionStart) / (this.SelectionEnd - this.SelectionStart);
			return true;
		}

		protected virtual bool SetLevel(double level)
		{
			return UpdateValue(this.SelectionStart + (this.SelectionEnd - this.SelectionStart) * level);
		}

		protected override bool UpdateValue(double value)
		{
			// Overriding CoerceValueCallback of ValueProperty will not be enough to limit the range
			// of value because it does not coerce a value sent to binding source.

			var adjustedValue = Math.Min(this.SelectionEnd, Math.Max(this.SelectionStart, value));
			if (this.Value == adjustedValue)
				return false;

			this.Value = adjustedValue;
			return true;
		}

		private static void OverrideMetadata()
		{
			Slider.SelectionStartProperty.OverrideMetadata(
				typeof(RangeSlider),
				new FrameworkPropertyMetadata(
					0D, // Equal to Minimum
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					(d, e) => ((RangeSlider)d).ReflectSelectionRange()));

			Slider.SelectionEndProperty.OverrideMetadata(
				typeof(RangeSlider),
				new FrameworkPropertyMetadata(
					100D, // Equal to Maximum
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					(d, e) => ((RangeSlider)d).ReflectSelectionRange()));
		}

		protected double GetRangeRate() => Math.Abs((this.SelectionEnd - this.SelectionStart) / (this.Maximum - this.Minimum));

		public double MinimumSelectionRange
		{
			get { return (double)GetValue(MinimumSelectionRangeProperty); }
			set { SetValue(MinimumSelectionRangeProperty, value); }
		}
		public static readonly DependencyProperty MinimumSelectionRangeProperty =
			DependencyProperty.Register(
				"MinimumSelectionRange",
				typeof(double),
				typeof(RangeSlider),
				new PropertyMetadata(1D));

		public bool IsSelectionRangeChanging
		{
			get { return (bool)GetValue(IsSelectionRangeChangingProperty); }
			set { SetValue(IsSelectionRangeChangingProperty, value); }
		}
		public static readonly DependencyProperty IsSelectionRangeChangingProperty =
			DependencyProperty.Register(
				"IsSelectionRangeChanging",
				typeof(bool),
				typeof(RangeSlider),
				new PropertyMetadata(
					false,
					(d, e) =>
					{
						var slider = (RangeSlider)d;
						slider.ReflectSelectionRange();

						if (!(bool)e.NewValue)
							slider.UpdateValue(slider.Value);
					}));

		private void ReflectSelectionRange()
		{
			this.IsSelectionRangeEnabled = IsSelectionRangeChanging
				|| (this.Minimum < this.SelectionStart) || (this.SelectionEnd < this.Maximum);
		}

		private enum Named
		{
			PART_StartTrack,
			PART_EndTrack
		}

		private Track _startTrack;
		private Track _endTrack;

		private void FindTemplateMembers()
		{
			ClearBindingAndRemoveDragEventHandler(_startTrack);
			ClearBindingAndRemoveDragEventHandler(_endTrack);

			_startTrack = this.GetTemplateChild(nameof(Named.PART_StartTrack)) as Track;
			_endTrack = this.GetTemplateChild(nameof(Named.PART_EndTrack)) as Track;

			SetBindingAndAddDragEventHandler(_startTrack, nameof(Slider.SelectionStart));
			SetBindingAndAddDragEventHandler(_endTrack, nameof(Slider.SelectionEnd));

			void SetBindingAndAddDragEventHandler(Track track, string propertyName)
			{
				if (track is null)
					return;

				var expression = BindingOperations.SetBinding(track, ValueProperty, new Binding(propertyName)
				{
					Source = this,
					Mode = BindingMode.TwoWay
				});
				//expression.UpdateTarget();

				track.Thumb.DragStarted += OnDragStarted;
				track.Thumb.DragDelta += OnDragDelta;
				track.Thumb.DragCompleted += OnDragCompleted;
			}

			void ClearBindingAndRemoveDragEventHandler(Track track)
			{
				if (track is null)
					return;

				BindingOperations.ClearBinding(track, ValueProperty);

				track.Thumb.DragStarted -= OnDragStarted;
				track.Thumb.DragDelta -= OnDragDelta;
				track.Thumb.DragCompleted -= OnDragCompleted;
			}
		}

		private void OnDragStarted(object sender, DragStartedEventArgs e)
		{
			if (!TryGetTrack(e, out Track track, out _))
				return;

			OpenToolTip(track);

			e.Handled = true; // This is necessary to prevent another ToolTip from being shown.
		}

		private void OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			if (!TryGetTrack(e, out Track track, out Func<double, double> filter))
				return;

			var newValue = track.Value + track.ValueFromDistance(e.HorizontalChange, e.VerticalChange);
			track.Value = filter(newValue);

			OpenToolTip(track);
		}

		private void OnDragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (!TryGetTrack(e, out Track track, out _))
				return;

			CloseToolTip(track);
		}

		private double FilterStartTrackValue(double value) => Math.Max(this.Minimum, Math.Min(this.SelectionEnd - this.MinimumSelectionRange, Math.Round(value)));
		private double FilterEndTrackValue(double value) => Math.Min(this.Maximum, Math.Max(this.SelectionStart + this.MinimumSelectionRange, Math.Round(value)));

		private bool TryGetTrack(RoutedEventArgs args, out Track track, out Func<double, double> filter)
		{
			if (args.OriginalSource is Thumb thumb)
			{
				if (_startTrack.Thumb == thumb)
				{
					track = _startTrack;
					filter = FilterStartTrackValue;
					return true;
				}
				if (_endTrack.Thumb == thumb)
				{
					track = _endTrack;
					filter = FilterEndTrackValue;
					return true;
				}
				//if (System.Windows.Media.VisualTreeHelper.GetParent(thumb) is Track buffer)
				//{
				//	track = buffer;
				//	filter = value => Math.Min(this.Maximum, Math.Max(this.Minimum, Math.Round(value)));
				//	return true;
				//}
			}
			track = default;
			filter = default;
			return false;
		}

		#endregion

		protected override bool SetValueStartDrag(
			Func<IInputElement, Point> getPosition,
			Action<UIElement> captureDevice)
		{
			return !IsSelectionRangeChanging
				&& base.SetValueStartDrag(getPosition, captureDevice);
		}

		protected override void OnThumbDragStarted(DragStartedEventArgs e)
		{
			if (!IsSelectionRangeChanging)
				base.OnThumbDragStarted(e);
		}

		protected override void OnThumbDragDelta(DragDeltaEventArgs e)
		{
			if (!IsSelectionRangeChanging)
				base.OnThumbDragDelta(e);
		}

		protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
		{
			if (!IsSelectionRangeChanging)
				base.OnThumbDragCompleted(e);
		}

		protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
		{
			if (!IsSelectionRangeChanging)
				base.OnManipulationDelta(e);
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (!IsSelectionRangeChanging)
				base.OnMouseWheel(e);
		}
	}
}