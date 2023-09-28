using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Monitorian.Core.Views.Controls;

public class IconButton : ToggleButton
{
	#region Icon

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

	#endregion

	#region Visibility

	private class IconButtonGroup
	{
		private readonly IconButton[] _buttons;

		public IconButtonGroup(IEnumerable<IconButton> buttons)
		{
			_buttons = buttons.Reverse().ToArray();

			CheckVisibleButtons();

			foreach (var b in _buttons)
				b.IsVisibleChanged += (_, _) => CheckVisibleButtons();
		}

		private void CheckVisibleButtons()
		{
			var isVisibleButtonFound = false;

			foreach (var b in _buttons)
			{
				if (!isVisibleButtonFound && b.IsVisible)
				{
					isVisibleButtonFound = true;
					b.IsLastVisibleButton = true;
				}
				else
				{
					b.IsLastVisibleButton = false;
				}
			}
		}
	}

	private IconButtonGroup _group;

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (this.Parent is Panel panel)
		{
			var buttons = panel.Children.OfType<IconButton>();
			if (buttons.All(x => x._group is null))
			{
				this._group = new IconButtonGroup(buttons);
			}
		}
	}

	public bool IsLastVisibleButton
	{
		get { return (bool)GetValue(IsLastVisibleButtonProperty); }
		set { SetValue(IsLastVisibleButtonProperty, value); }
	}
	public static readonly DependencyProperty IsLastVisibleButtonProperty =
		DependencyProperty.Register(
			"IsLastVisibleButton",
			typeof(bool),
			typeof(IconButton),
			new PropertyMetadata(false));

	#endregion

	private UIElement _parent;

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		base.OnMouseDown(e);

		var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, e.ChangedButton) { RoutedEvent = Mouse.MouseDownEvent };
		_parent ??= VisualTreeHelper.GetParent(this) as UIElement;
		_parent?.RaiseEvent(args);
	}
}