using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Monitorian.Core.Views.Controls
{
	[TemplatePart(Name = "PART_ShadowThumb", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PART_ShadowLeft", Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = "PART_ShadowRight", Type = typeof(ColumnDefinition))]
	public class CompoundSlider : EnhancedSlider
	{
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			CheckCanUseShadow();
			ReflectShadow();
		}

		#region Shadow

		public bool IsShadowVisible
		{
			get { return (bool)GetValue(IsShadowVisibleProperty); }
			set { SetValue(IsShadowVisibleProperty, value); }
		}
		public static readonly DependencyProperty IsShadowVisibleProperty =
			DependencyProperty.Register(
				"IsShadowVisible",
				typeof(bool),
				typeof(CompoundSlider),
				new PropertyMetadata(
					false,
					(d, e) => ((CompoundSlider)d).ReflectShadow()));

		public double ValueShadow
		{
			get { return (double)GetValue(ValueShadowProperty); }
			set { SetValue(ValueShadowProperty, value); }
		}
		public static readonly DependencyProperty ValueShadowProperty =
			DependencyProperty.Register(
				"ValueShadow",
				typeof(double),
				typeof(CompoundSlider),
				new PropertyMetadata(
					-1D,
					(d, e) => ((CompoundSlider)d).ReflectShadow()));

		protected bool CanUseShadow { get; private set; }

		private void CheckCanUseShadow()
		{
			CanUseShadow = FindTemplateMembers();
		}

		private FrameworkElement _shadowThumb;
		private ColumnDefinition _shadowLeft;
		private ColumnDefinition _shadowRight;

		private bool FindTemplateMembers()
		{
			_shadowThumb = this.GetTemplateChild("PART_ShadowThumb") as FrameworkElement;
			_shadowLeft = this.GetTemplateChild("PART_ShadowLeft") as ColumnDefinition;
			_shadowRight = this.GetTemplateChild("PART_ShadowRight") as ColumnDefinition;

			return (_shadowThumb != null)
				&& (_shadowLeft != null)
				&& (_shadowRight != null);
		}

		private void ReflectShadow()
		{
			if (!CanUseShadow)
				return;

			if (!IsShadowVisible || (ValueShadow < 0) || (ValueShadow == this.Value))
			{
				_shadowThumb.Visibility = Visibility.Collapsed;
				return;
			}
			else
			{
				_shadowThumb.Visibility = Visibility.Visible;
			}

			var ratio = Math.Min(ValueShadow / (this.Maximum - this.Minimum), 1D);

			_shadowLeft.Width = new GridLength(ratio, GridUnitType.Star);
			_shadowRight.Width = new GridLength(1D - ratio, GridUnitType.Star);
		}

		#endregion

		#region Unison

		private static event EventHandler<double> Moved; // Static event

		public bool IsUnison
		{
			get { return (bool)GetValue(IsUnisonProperty); }
			set { SetValue(IsUnisonProperty, value); }
		}
		public static readonly DependencyProperty IsUnisonProperty =
			DependencyProperty.Register(
				"IsUnison",
				typeof(bool),
				typeof(CompoundSlider),
				new PropertyMetadata(
					false,
					(d, e) =>
					{
						var instance = (CompoundSlider)d;

						if ((bool)e.NewValue)
						{
							Moved += instance.OnMoved;
						}
						else
						{
							Moved -= instance.OnMoved;
						}
					}));

		public int ValueUnison
		{
			get { return (int)GetValue(ValueUnisonProperty); }
			set { SetValue(ValueUnisonProperty, value); }
		}
		public static readonly DependencyProperty ValueUnisonProperty =
			DependencyProperty.Register(
				"ValueUnison",
				typeof(int),
				typeof(CompoundSlider),
				new PropertyMetadata(
					0,
					(d, e) =>
					{
						var instance = (CompoundSlider)d;

						if (!instance.IsFocused && instance.IsUnison)
						{
							Moved?.Invoke(instance, (int)e.NewValue - instance.Value);
						}
					}));

		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);

			if (this.IsFocused && IsUnison)
			{
				Moved?.Invoke(this, newValue - oldValue);
			}
		}

		private double? _brightnessProtruded = null;

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			_brightnessProtruded = null; // Reset
		}

		private void OnMoved(object sender, double delta)
		{
			if (ReferenceEquals(this, sender) || (delta == 0D))
				return;

			_brightnessProtruded ??= this.Value;
			_brightnessProtruded += delta;

			this.Value = Math.Min(this.Maximum, Math.Max(this.Minimum, _brightnessProtruded.Value));
		}

		#endregion
	}
}