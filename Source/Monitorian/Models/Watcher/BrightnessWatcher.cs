using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Watcher
{
	internal class BrightnessWatcher : IDisposable
	{
		private readonly ManagementEventWatcher _watcher;
		private Action<string, int> _onChanged;

		public BrightnessWatcher()
		{
			var scope = @"root\wmi";
			var query = "SELECT * FROM WmiMonitorBrightnessEvent";
			var option = new EventWatcherOptions(null, TimeSpan.FromSeconds(1), 1);
			_watcher = new ManagementEventWatcher(scope, query, option);
		}

		public void Subscribe(Action<string, int> onChanged)
		{
			if (onChanged == null)
				throw new ArgumentNullException(nameof(onChanged));

			this._onChanged = onChanged;
			_watcher.EventArrived += OnEventArrived;
			_watcher.Start();
		}

		private void OnEventArrived(object sender, EventArrivedEventArgs e)
		{
			var newEvent = e.NewEvent;
			var instanceName = (string)newEvent["InstanceName"];
			var brightness = (byte)newEvent["Brightness"];

			_onChanged?.Invoke(instanceName, brightness);
		}

		#region IDisposable

		private bool _isDisposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				_watcher.Stop();
				_watcher.EventArrived -= OnEventArrived;
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}