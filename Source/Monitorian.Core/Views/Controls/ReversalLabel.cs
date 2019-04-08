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

		public double CornerRadius
		{
			get { return (double)GetValue(CornerRadiusProperty); }
			set { SetValue(CornerRadiusProperty, value); }
		}
		public static readonly DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register(
				"CornerRadius",
				typeof(double),
				typeof(ReversalLabel),
				new PropertyMetadata(
					0D,
					(d, e) => ((UIElement)d).InvalidateVisual()));

		#endregion

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			var (_, labelSize) = CreateFormattedText();

			this.Width = labelSize.Width;
			this.Height = labelSize.Height;

			return labelSize;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			try
			{
				var (formattedText, labelSize) = CreateFormattedText();

				var textGeometry = formattedText.BuildGeometry(new Point(this.Padding.Left, this.Padding.Top));
				var backgroundGeometry = new RectangleGeometry(new Rect(labelSize), this.CornerRadius, this.CornerRadius);
				var combinedGeometry = new CombinedGeometry(GeometryCombineMode.Xor, textGeometry, backgroundGeometry);

				drawingContext.DrawGeometry(this.Background ?? Brushes.Black, null, combinedGeometry);
			}
			finally
			{
				ClearFormattedText();
			}
		}

		private FormattedText _formattedText;
		private Size _labelSize;

		private (FormattedText formattedText, Size labelSize) CreateFormattedText()
		{
			FormattedText GetFormattedText() => new FormattedText(
				this.Text ?? string.Empty,
				CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
				this.FontSize,
				Brushes.Black, // Not to be used
				(0 < _dpi.PixelsPerDip ? _dpi.PixelsPerDip : 1D));

			Size GetLabelSize(FormattedText formattedText)
			{
				var width = this.Padding.Left + formattedText.Width + this.Padding.Right;
				var height = this.Padding.Top + formattedText.Height + this.Padding.Bottom;

				if (this.SnapsToDevicePixels)
					(width, height) = (Math.Round(width, MidpointRounding.AwayFromZero), Math.Round(height, MidpointRounding.AwayFromZero));

				return new Size(width, height);
			}

			return (_formattedText is null)
				? (_formattedText = GetFormattedText(), _labelSize = GetLabelSize(_formattedText))
				: (_formattedText, _labelSize);
		}

		private void ClearFormattedText() => _formattedText = null;
	}
}