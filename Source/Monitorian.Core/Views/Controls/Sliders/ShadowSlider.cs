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
	public class ShadowSlider : RangeSlider
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
				typeof(ShadowSlider),
				new PropertyMetadata(
					false,
					(d, e) => ((ShadowSlider)d).ReflectShadow()));

		public double ValueShadow
		{
			get { return (double)GetValue(ValueShadowProperty); }
			set { SetValue(ValueShadowProperty, value); }
		}
		public static readonly DependencyProperty ValueShadowProperty =
			DependencyProperty.Register(
				"ValueShadow",
				typeof(double),
				typeof(ShadowSlider),
				new PropertyMetadata(
					-1D,
					(d, e) => ((ShadowSlider)d).ReflectShadow()));

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

			return (_shadowThumb is not null)
				&& (_shadowLeft is not null)
				&& (_shadowRight is not null);
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
	}
}