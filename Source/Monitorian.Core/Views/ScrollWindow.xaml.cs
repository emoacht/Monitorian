using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views.Controls;
using ScreenFrame.Movers;

namespace Monitorian.Core.Views;

public partial class ScrollWindow : Window
{
	private readonly FloatWindowMover _mover;

	public ScrollWindow(AppControllerCore controller, Point pivot)
	{
		LanguageService.Switch();

		InitializeComponent();

		this.DataContext = new ScrollWindowViewModel(controller);

		_mover = new FloatWindowMover(this, pivot);

		controller.WindowPainter.Add(this);
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		FlowElement.EnsureFlowDirection(this);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.Key is Key.Escape)
			OnCloseTriggered(this, EventArgs.Empty);
	}

	#region Close

	private bool _isClosing = false;

	protected void OnCloseTriggered(object sender, EventArgs e)
	{
		if (!_isClosing && this.IsLoaded)
			this.Close();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);

		if (!_isClosing)
			this.Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!e.Cancel)
		{
			_isClosing = true;

			if (this.DataContext is IDisposable disposable)
				disposable.Dispose();
		}

		base.OnClosing(e);
	}

	#endregion
}