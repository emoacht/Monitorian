using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Monitorian.Core.Views;

public partial class OsdWindow : Window
{
	private DispatcherTimer _fadeTimer;

	public OsdWindow()
	{
		InitializeComponent();

		_fadeTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(1500)
		};
		_fadeTimer.Tick += OnFadeTimerTick;
	}

	public void ShowValue(int value, Point pivot)
	{
		ValueText.Text = $"{value}%";

		// Position slightly above the tray icon
		this.Left = pivot.X - (this.Width / 2);
		this.Top = pivot.Y - this.Height - 10;

		this.Show();

		// Fade in
		var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
		this.BeginAnimation(UIElement.OpacityProperty, fadeIn);

		_fadeTimer.Stop();
		_fadeTimer.Start();
	}

	private void OnFadeTimerTick(object sender, EventArgs e)
	{
		_fadeTimer.Stop();

		// Fade out
		var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
		fadeOut.Completed += (s, eArgs) => this.Hide();
		this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
	}
}
