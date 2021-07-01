using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Monitorian.Core.Views.Controls
{
	public class IconButton : ToggleButton
	{
		public Geometry StrokeIconData
		{
			get { return (Geometry)GetValue(StrokeIconDataProperty); }
			set { SetValue(StrokeIconDataProperty, value); }
		}
		public static readonly DependencyProperty StrokeIconDataProperty =
			DependencyProperty.Register(
				"StrokeIconData",
				typeof(Geometry),
				typeof(IconButton),
				new PropertyMetadata(null));

		public Geometry FillIconData
		{
			get { return (Geometry)GetValue(FillIconDataProperty); }
			set { SetValue(FillIconDataProperty, value); }
		}
		public static readonly DependencyProperty FillIconDataProperty =
			DependencyProperty.Register(
				"FillIconData",
				typeof(Geometry),
				typeof(IconButton),
				new PropertyMetadata(null));

		private UIElement _parent;

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, e.ChangedButton) { RoutedEvent = Mouse.MouseDownEvent };
			_parent ??= VisualTreeHelper.GetParent(this) as UIElement;
			_parent?.RaiseEvent(args);
		}
	}
}