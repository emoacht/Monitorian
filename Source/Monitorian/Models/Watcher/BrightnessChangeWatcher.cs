using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Watcher
{
	internal class BrightnessChangeWatcher
	{
		private readonly ManagementEventWatcher _watcher;
		private Action<string, int> _action;

		public BrightnessChangeWatcher()
		{
			var scope = @"root\wmi";
			var query = "SELECT * FROM WmiMonitorBrightnessEvent";
			var option = new EventWatcherOptions(null, TimeSpan.FromSeconds(1), 1);
			_watcher = new ManagementEventWatcher(scope, query, option);
		}

		public void Start(Action<string, int> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_action = action;
			_watcher.EventArrived += OnEventArrived;
			_watcher.Start();
		}

		public void Stop()
		{
			_watcher.EventArrived -= OnEventArrived;
			_watcher.Stop();
		}

		private void OnEventArrived(object sender, EventArrivedEventArgs e)
		{
			var newEvent = e.NewEvent;
			var instanceName = (string)newEvent["InstanceName"];
			var brightness = (byte)newEvent["Brightness"];

			_action?.Invoke(instanceName, brightness);
		}
	}
}