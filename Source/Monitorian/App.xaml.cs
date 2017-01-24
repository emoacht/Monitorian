using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Monitorian.Models;

namespace Monitorian
{
	public partial class App : Application
	{
		private RemotingAgent _agent;
		private MainController _controller;

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			ConsoleLogService.Start();

			if (e.Args.Contains(RegistryService.Argument))
				await Task.Delay(TimeSpan.FromSeconds(10));

			_agent = new RemotingAgent();
			if (!_agent.Start())
			{
				this.Shutdown();
				return;
			}

			LanguageService.Switch(e.Args);

			_controller = new MainController();
			await _controller.InitiateAsync(_agent);

			//this.MainWindow = new MainWindow();
			//this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_agent?.End();
			_controller?.End();

			ConsoleLogService.End();

			base.OnExit(e);
		}
	}
}