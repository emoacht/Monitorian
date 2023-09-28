using System.Windows;

using Monitorian.Core;

namespace Monitorian;

public partial class App : Application
{
	private AppKeeper _keeper;
	private AppController _controller;

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		_keeper = new AppKeeper();
		if (!await _keeper.StartAsync(e))
		{
			this.Shutdown(0); // This shutdown is expected behavior.
			return;
		}

		_controller = new AppController(_keeper);
		await _controller.InitiateAsync();

		//this.MainWindow = new MainWindow();
		//this.MainWindow.Show();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_controller?.End();
		_keeper.End();

		base.OnExit(e);
	}
}