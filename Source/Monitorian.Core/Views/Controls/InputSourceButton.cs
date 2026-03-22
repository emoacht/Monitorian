using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views;

namespace Monitorian.Core.Views.Controls;

/// <summary>
/// A button that shows a popup for input source selection
/// </summary>
public class InputSourceButton : ToggleButton
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
			typeof(InputSourceButton),
			new PropertyMetadata(defaultValue: null));

	public Geometry FillIconData
	{
		get { return (Geometry)GetValue(FillIconDataProperty); }
		set { SetValue(FillIconDataProperty, value); }
	}
	public static readonly DependencyProperty FillIconDataProperty =
		DependencyProperty.Register(
			"FillIconData",
			typeof(Geometry),
			typeof(InputSourceButton),
			new PropertyMetadata(defaultValue: null));

	#endregion

	#region Visibility

	public bool IsLastVisibleButton
	{
		get { return (bool)GetValue(IsLastVisibleButtonProperty); }
		set { SetValue(IsLastVisibleButtonProperty, value); }
	}
	public static readonly DependencyProperty IsLastVisibleButtonProperty =
		DependencyProperty.Register(
			"IsLastVisibleButton",
			typeof(bool),
			typeof(InputSourceButton),
			new PropertyMetadata(false));

	#endregion

	private Popup _popup;
	private ItemsControl _itemsControl;
	private UIElement _parent;

	public InputSourceButton()
	{
		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		CreatePopup();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_popup != null)
		{
			_popup.Opened -= OnPopupOpened;
			_popup.Closed -= OnPopupClosed;
		}
	}

	private void CreatePopup()
	{
		if (_popup != null)
			return;

		_itemsControl = new ItemsControl
		{
			Background = (Brush)FindResource("App.Background.Plain"),
			Foreground = (Brush)FindResource("App.Foreground"),
			MinWidth = 100
		};

		_popup = new Popup
		{
			PlacementTarget = this,
			Placement = PlacementMode.Bottom,
			StaysOpen = false,
			AllowsTransparency = true,
			Child = new Border
			{
				Background = (Brush)FindResource("App.Background.Plain"),
				BorderBrush = (Brush)FindResource("App.Foreground"),
				BorderThickness = new Thickness(1),
				Padding = new Thickness(4),
				Child = _itemsControl
			}
		};

		_popup.Opened += OnPopupOpened;
		_popup.Closed += OnPopupClosed;
	}

	protected override void OnChecked(RoutedEventArgs e)
	{
		base.OnChecked(e);

		if (_popup != null && DataContext is MonitorViewModel vm)
		{
			vm.UpdateInputSource();
			PopulateInputSources(vm);
			_popup.IsOpen = true;
		}
	}

	protected override void OnUnchecked(RoutedEventArgs e)
	{
		base.OnUnchecked(e);

		if (_popup != null)
		{
			_popup.IsOpen = false;
		}
	}

	private void OnPopupOpened(object sender, EventArgs e)
	{
		// Keep checked state while popup is open
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		IsChecked = false;
	}

	private void PopulateInputSources(MonitorViewModel vm)
	{
		_itemsControl.Items.Clear();

		var enabledSources = vm.GetEnabledInputSources();

		foreach (var source in enabledSources)
		{
			var isCurrentInput = source.Value == vm.CurrentInputSource;
			var button = new Button
			{
				Content = source.Label,
				Tag = source.Value,
				Padding = new Thickness(8, 4, 8, 4),
				Margin = new Thickness(0, 2, 0, 2),
				HorizontalContentAlignment = HorizontalAlignment.Left,
				Background = isCurrentInput
					? new SolidColorBrush((Color)FindResource("App.Background.Accent.StaticColor"))
					: Brushes.Transparent,
				Foreground = (Brush)FindResource("App.Foreground"),
				BorderThickness = new Thickness(0),
				Cursor = Cursors.Hand
			};

			button.Click += (s, e) =>
			{
				if (s is Button btn && btn.Tag is byte value)
				{
					vm.SetInputSource(value);
					_popup.IsOpen = false;
				}
			};

			_itemsControl.Items.Add(button);
		}

		// Add configure option at the bottom
		var separator = new Separator
		{
			Margin = new Thickness(0, 4, 0, 4)
		};
		_itemsControl.Items.Add(separator);

		var configureButton = new Button
		{
			Content = "Configure...",
			Padding = new Thickness(8, 4, 8, 4),
			Margin = new Thickness(0, 2, 0, 2),
			HorizontalContentAlignment = HorizontalAlignment.Left,
			Background = Brushes.Transparent,
			Foreground = (Brush)FindResource("App.Foreground"),
			BorderThickness = new Thickness(0),
			Cursor = Cursors.Hand,
			FontStyle = FontStyles.Italic
		};

		configureButton.Click += (s, e) =>
		{
			_popup.IsOpen = false;
			ShowConfigurationDialog(vm);
		};

		_itemsControl.Items.Add(configureButton);
	}

	private void ShowConfigurationDialog(MonitorViewModel vm)
	{
		var window = new InputSourceConfigWindow(vm)
		{
			Owner = Window.GetWindow(this)
		};
		window.ShowDialog();
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		base.OnMouseDown(e);

		var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, e.ChangedButton) { RoutedEvent = Mouse.MouseDownEvent };
		_parent ??= VisualTreeHelper.GetParent(this) as UIElement;
		_parent?.RaiseEvent(args);
	}
}
