using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views.Controls;
using ScreenFrame.Movers;

namespace Monitorian.Core.Views;

public partial class MenuWindow : Window
{
	private readonly FloatWindowMover _mover;
	private readonly AppControllerCore _controller;
	public MenuWindowViewModel ViewModel => (MenuWindowViewModel)this.DataContext;

	public MenuWindow(AppControllerCore controller, Point pivot)
	{
		LanguageService.Switch();

		InitializeComponent();

		this._controller = controller;
		this.DataContext = new MenuWindowViewModel(controller);

		_mover = new FloatWindowMover(this, pivot);
		_mover.ForegroundWindowChanged += OnDeactivated;
		_mover.AppDeactivated += OnDeactivated;

		controller.WindowPainter.Add(this);
	}

	public UIElementCollection HeadSection => this.HeadItems.Children;
	public UIElementCollection MenuSectionTop => this.MenuItemsTop.Children;
	public UIElementCollection MenuSectionMiddle => this.MenuItemsMiddle.Children;

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		FlowElement.EnsureFlowDirection(this);

		// Initialize shortcut text
		IncreaseKeyBox.Text = FormatShortcut(ViewModel.Settings.IncreaseBrightnessModifiers, ViewModel.Settings.IncreaseBrightnessKey);
		DecreaseKeyBox.Text = FormatShortcut(ViewModel.Settings.DecreaseBrightnessModifiers, ViewModel.Settings.DecreaseBrightnessKey);
	}

	private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
	{
		var textBox = (TextBox)sender;
		textBox.Text = "Press a shortcut...";
		textBox.BorderBrush = System.Windows.Media.Brushes.DeepSkyBlue; // Highlight color
	}

	private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		var textBox = (TextBox)sender;
		textBox.BorderBrush = System.Windows.Media.Brushes.Gray; // Restore default

		if (textBox.Tag.ToString() == "Increase")
			textBox.Text = FormatShortcut(ViewModel.Settings.IncreaseBrightnessModifiers, ViewModel.Settings.IncreaseBrightnessKey);
		else
			textBox.Text = FormatShortcut(ViewModel.Settings.DecreaseBrightnessModifiers, ViewModel.Settings.DecreaseBrightnessKey);
	}

	private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		e.Handled = true;
		var textBox = (TextBox)sender;

		// Extract true key (handling system keys like Alt shortcuts)
		var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

		// Cancel on Escape
		if (key == System.Windows.Input.Key.Escape)
		{
			System.Windows.Input.Keyboard.ClearFocus();
			return;
		}

		// Clear on Backspace or Delete
		if (key == System.Windows.Input.Key.Back || key == System.Windows.Input.Key.Delete)
		{
			if (textBox.Tag.ToString() == "Increase")
			{
				ViewModel.Settings.IncreaseBrightnessModifiers = 0;
				ViewModel.Settings.IncreaseBrightnessKey = 0;
			}
			else
			{
				ViewModel.Settings.DecreaseBrightnessModifiers = 0;
				ViewModel.Settings.DecreaseBrightnessKey = 0;
			}
			System.Windows.Input.Keyboard.ClearFocus();
			return;
		}

		// Ignore modifier-only presses (Wait for the user to hit an actual key)
		if (key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
			key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
			key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
			key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
		{
			return;
		}

		// Successfully captured a full chord
		var modifiers = System.Windows.Input.Keyboard.Modifiers;

		if (textBox.Tag.ToString() == "Increase")
		{
			ViewModel.Settings.IncreaseBrightnessModifiers = (int)modifiers;
			ViewModel.Settings.IncreaseBrightnessKey = (int)key;
		}
		else
		{
			ViewModel.Settings.DecreaseBrightnessModifiers = (int)modifiers;
			ViewModel.Settings.DecreaseBrightnessKey = (int)key;
		}

		// Drop focus to trigger LostFocus (which updates UI and formats text)
		System.Windows.Input.Keyboard.ClearFocus();
	}

	private string FormatShortcut(int modifiersInt, int keyInt)
	{
		if (keyInt == 0) return "None";
		var modifiers = (ModifierKeys)modifiersInt;
		var key = (Key)keyInt;

		string text = "";
		if (modifiers.HasFlag(ModifierKeys.Windows)) text += "Win + ";
		if (modifiers.HasFlag(ModifierKeys.Control)) text += "Ctrl + ";
		if (modifiers.HasFlag(ModifierKeys.Alt)) text += "Alt + ";
		if (modifiers.HasFlag(ModifierKeys.Shift)) text += "Shift + ";

		// Clean up common key names for UI
		var keyStr = key.ToString();
		if (key >= Key.D0 && key <= Key.D9) keyStr = keyStr.TrimStart('D');

		text += keyStr;
		return text;
	}

	private void InvertScrollDirection_Click(object sender, RoutedEventArgs e)
	{
		if (sender is ButtonBase button)
		{
			var topLeft = button.PointToScreen(new Point(0, 0));
			var bottomRight = button.PointToScreen(new Point(button.ActualWidth, button.ActualHeight));
			var pivot = new Rect(topLeft, bottomRight);

			DepartFromForeground();

			var scrollWindow = new ScrollWindow(_controller, pivot);
			scrollWindow.Closed += OnClosed;
			scrollWindow.Show();
		}

		void OnClosed(object sender, EventArgs e)
		{
			((Window)sender).Closed -= OnClosed;
			ReturnToForeground();
		}
	}

	#region Show/Close

	public void DepartFromForeground()
	{
		this.Topmost = false;
	}

	public async void ReturnToForeground()
	{
		// Wait for this window to be able to be activated.
		await Task.Delay(TimeSpan.FromMilliseconds(100));

		if (_isClosing)
			return;

		// Activate this window. This is necessary to assure this window is foreground.
		this.Activate();

		this.Topmost = true;
	}

	private bool _isClosing = false;

	private void OnDeactivated(object sender, EventArgs e)
	{
		if (!_isClosing && this.IsLoaded)
			this.Close();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);

		if (!this.Topmost)
			return;

		if (!_isClosing)
			this.Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!e.Cancel)
		{
			_isClosing = true;
			ViewModel.Dispose();
		}

		base.OnClosing(e);
	}

	#endregion
}