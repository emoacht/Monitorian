using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using ScreenFrame.Movers;

namespace Monitorian.Core.Views;

public partial class MainWindow : Window
{
	private readonly StickWindowMover _mover;
	public MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

	public MainWindow(AppControllerCore controller)
	{
		LanguageService.Switch();

		InitializeComponent();

		this.DataContext = new MainWindowViewModel(controller);

		_mover = new StickWindowMover(this, controller.NotifyIconContainer.NotifyIcon)
		{
			KeepsDistance = true
		};
		_mover.ForegroundWindowChanged += OnDeactivated;

		controller.WindowPainter.Add(this);
		controller.WindowPainter.ThemeChanged += (_, _) =>
		{
			ViewModel.MonitorsView.Refresh();
		};
		//controller.WindowPainter.AccentColorChanged += (_, _) =>
		//{
		//};
	}

	#region Show/Hide

	internal Point? CursorLocation
	{
		get => _mover.CursorLocation;
		set => _mover.CursorLocation = value;
	}

	public bool IsForeground => _mover.IsForeground();

	public void ShowForeground()
	{
		try
		{
			this.Topmost = true;

			// When a window is deactivated, a focused element will lose focus and usually,
			// no element will have focus until the window is activated again and the last focused
			// element automatically gets focus back. Therefore, in usual case, no focused element
			// exists before Window.Show method is called. However, it is possible to set focus on
			// an element during window is not active and such focused element is found here.
			// The issue is that such focused element will lose focus because the element which had
			// focus before the window was deactivated will restore focus even though any other
			// element has focus. To prevent this unintended change of focus, it is necessary to
			// set focus back on the element which has focus before Window.Show method is called.
			var currentFocusedElement = FocusManager.GetFocusedElement(this);

			base.Show();

			if (currentFocusedElement is not null)
			{
				var restoredFocusedElement = FocusManager.GetFocusedElement(this);
				if (restoredFocusedElement != currentFocusedElement)
					FocusManager.SetFocusedElement(this, currentFocusedElement);
			}

			// Set time to prevent hiding procedure from being triggered.
			_preventionTime = DateTimeOffset.Now + TimeSpan.FromSeconds(0.1);
		}
		catch (ArgumentException ex) when ((uint)ex.HResult is 0x80070057)
		{
			// Window.Show method can cause ArgumentException when internally calling
			// CompositionTarget.SetRootVisual method.
		}
		finally
		{
			this.Topmost = false;
		}
	}

	public void ShowUnnoticed()
	{
		var width = this.Width;
		var height = this.Height;
		var sizeToContent = this.SizeToContent;
		try
		{
			// Set window size as small as possible to make it almost unnoticed.
			this.Width = 1;
			this.Height = 1;
			this.SizeToContent = SizeToContent.Manual;

			base.Show();
			this.Hide();
		}
		finally
		{
			// Restore window size.
			this.Width = width;
			this.Height = height;
			this.SizeToContent = sizeToContent;
		}
	}

	public bool CanBeShown => (_preventionTime < DateTimeOffset.Now);
	private DateTimeOffset _preventionTime;

	private void OnDeactivated(object sender, EventArgs e)
	{
		ProceedHide();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);

		ProceedHide();
	}

	private void ProceedHide()
	{
		if (this.Visibility is not Visibility.Visible)
			return;

		// Compare time to prevent hiding procedure from repeating.
		if (_preventionTime > DateTimeOffset.Now)
			return;

		ViewModel.Deactivate();

		// Set time to prevent this window from being shown unintentionally.
		_preventionTime = DateTimeOffset.Now + TimeSpan.FromSeconds(0.2);

		ClearHide();
	}

	public async void ClearHide()
	{
		// Clear focus.
		FocusManager.SetFocusedElement(this, null);

		// Wait for this window to be refreshed before being hidden.
		await Task.Delay(TimeSpan.FromSeconds(0.1));

		this.Hide();
	}

	#endregion
}