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
		private FrameworkElement _shadowThumb;
		private ColumnDefinition _shadowLeft;
		private ColumnDefinition _shadowRight;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_shadowThumb = this.GetTemplateChild("PART_ShadowThumb") as FrameworkElement;
			_shadowLeft = this.GetTemplateChild("PART_ShadowLeft") as ColumnDefinition;
			_shadowRight = this.GetTemplateChild("PART_ShadowRight") as ColumnDefinition;

			ReflectShadowValue();
		}

		private void ReflectShadowValue()
		{
			if ((_shadowThumb is null) || (_shadowLeft is null) || (_shadowRight is null))
				return;

			if ((ShadowVisibility != Visibility.Visible) || (ShadowValue < 0) || (ShadowValue == this.Value))
			{
				_shadowThumb.Visibility = Visibility.Collapsed;
				return;
			}
			else
			{
				_shadowThumb.Visibility = Visibility.Visible;
			}

			var ratio = Math.Min(ShadowValue / (this.Maximum - this.Minimum), 1D);

			_shadowLeft.Width = new GridLength(ratio, GridUnitType.Star);
			_shadowRight.Width = new GridLength(1D - ratio, GridUnitType.Star);
		}

		public double ShadowValue
		{
			get { return (double)GetValue(ShadowValueProperty); }
			set { SetValue(ShadowValueProperty, value); }
		}
		public static readonly DependencyProperty ShadowValueProperty =
			DependencyProperty.Register(
				"ShadowValue",
				typeof(double),
				typeof(CompoundSlider),
				new PropertyMetadata(
					-1D,
					(d, e) => ((CompoundSlider)d).ReflectShadowValue()));

		public Visibility ShadowVisibility
		{
			get { return (Visibility)GetValue(ShadowVisibilityProperty); }
			set { SetValue(ShadowVisibilityProperty, value); }
		}
		public static readonly DependencyProperty ShadowVisibilityProperty =
			DependencyProperty.Register(
				"ShadowVisibility",
				typeof(Visibility),
				typeof(CompoundSlider),
				new PropertyMetadata(
					Visibility.Collapsed,
					(d, e) => ((CompoundSlider)d).ReflectShadowValue()));
	}
}