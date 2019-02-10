using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Monitorian.Models;
using StartupAgency;

namespace Monitorian
{
	public partial class App : Application
	{
		private StartupAgent _agent;
		private MainController _controller;

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			TrapService.Start();

			_agent = new StartupAgent();
			if (!_agent.Start(ProductInfo.StartupTaskId))
			{
				this.Shutdown(0); // This shutdown is expected behavior.
				return;
			}

			_controller = new MainController(_agent);
			await _controller.InitiateAsync();

			//this.MainWindow = new MainWindow();
			//this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_controller?.End();
			_agent.Dispose();

			TrapService.End();

			base.OnExit(e);
		}
	}
}