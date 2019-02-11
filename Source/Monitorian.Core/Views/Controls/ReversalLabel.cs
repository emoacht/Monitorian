using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Monitorian.Core.Views.Controls
{
	public class ReversalLabel : Control
	{
		private DpiScale _dpi;

		protected override void OnInitialized(EventArgs e)
		{
			_dpi = VisualTreeHelper.GetDpi(this);

			base.OnInitialized(e);
		}

		protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
		{
			_dpi = newDpi;

			base.OnDpiChanged(oldDpi, newDpi);
		}

		#region Property

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		protected static readonly DependencyProperty TextProperty =
			TextBlock.TextProperty.AddOwner(
				typeof(ReversalLabel),
				new PropertyMetadata(
					string.Empty,
					(d, e) => ((UIElement)d).InvalidateVisual()));

		public double Radius
		{
			get { return (double)GetValue(RadiusProperty); }
			set { SetValue(RadiusProperty, value); }
		}
		public static readonly DependencyProperty RadiusProperty =
			DependencyProperty.Register(
				"Radius",
				typeof(double),
				typeof(ReversalLabel),
				new PropertyMetadata(
					0D,
					(d, e) => ((UIElement)d).InvalidateVisual()));

		#endregion

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			CreateFormattedText(out _, out _, out Size size);

			this.Width = size.Width;
			this.Height = size.Height;

			return size;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			try
			{
				CreateFormattedText(out FormattedText formattedText, out double padding, out Size size);

				var textGeometry = formattedText.BuildGeometry(new Point(padding, 0));
				var backgroundGeometry = new RectangleGeometry(new Rect(size), this.Radius, this.Radius);
				var combinedGeometry = new CombinedGeometry(GeometryCombineMode.Xor, textGeometry, backgroundGeometry);

				drawingContext.DrawGeometry(this.Foreground ?? Brushes.Black, null, combinedGeometry);
			}
			finally
			{
				ClearFormattedText();
			}
		}

		private FormattedText _formattedText;

		private void CreateFormattedText(out FormattedText formattedText, out double padding, out Size size)
		{
			formattedText = _formattedText ?? (_formattedText = new FormattedText(
				this.Text ?? string.Empty,
				CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
				this.FontSize,
				Brushes.Black, // Not to be used
				(0 < _dpi.PixelsPerDip ? _dpi.PixelsPerDip : 1D)));

			padding = (formattedText.Height - formattedText.Extent) / 2D;
			size = new Size(formattedText.Width + padding * 2, formattedText.Height);
		}

		private void ClearFormattedText() => _formattedText = null;
	}
}