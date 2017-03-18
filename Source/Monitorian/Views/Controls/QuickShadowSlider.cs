using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Monitorian.Views.Controls
{
	[TemplatePart(Name = "PART_ShadowCenter", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PART_ShadowLeft", Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = "PART_ShadowRight", Type = typeof(ColumnDefinition))]
	public class QuickShadowSlider : QuickSlider
	{
		private FrameworkElement _shadowCenter;
		private ColumnDefinition _shadowLeft;
		private ColumnDefinition _shadowRight;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_shadowCenter = this.GetTemplateChild("PART_ShadowCenter") as FrameworkElement;
			_shadowLeft = this.GetTemplateChild("PART_ShadowLeft") as ColumnDefinition;
			_shadowRight = this.GetTemplateChild("PART_ShadowRight") as ColumnDefinition;

			ReflectShadowValue();
		}

		private void ReflectShadowValue()
		{
			if ((_shadowCenter == null) || (_shadowLeft == null) || (_shadowRight == null))
				return;

			if ((ShadowVisibility != Visibility.Visible) || (ShadowValue < 0) || (ShadowValue == this.Value))
			{
				_shadowCenter.Visibility = Visibility.Collapsed;
				return;
			}
			else
			{
				_shadowCenter.Visibility = Visibility.Visible;
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
				typeof(QuickShadowSlider),
				new PropertyMetadata(
					-1D,
					(d, e) => ((QuickShadowSlider)d).ReflectShadowValue()));

		public Visibility ShadowVisibility
		{
			get { return (Visibility)GetValue(ShadowVisibilityProperty); }
			set { SetValue(ShadowVisibilityProperty, value); }
		}
		public static readonly DependencyProperty ShadowVisibilityProperty =
			DependencyProperty.Register(
				"ShadowVisibility",
				typeof(Visibility),
				typeof(QuickShadowSlider),
				new PropertyMetadata(
					Visibility.Collapsed,
					(d, e) => ((QuickShadowSlider)d).ReflectShadowValue()));
	}
}