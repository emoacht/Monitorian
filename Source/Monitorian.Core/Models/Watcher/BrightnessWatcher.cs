using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.Models.Watcher
{
	internal class BrightnessWatcher : IDisposable
	{
		private ManagementEventWatcher _watcher;
		private Action<string, int> _onBrightnessChanged;

		public BrightnessWatcher()
		{ }

		/// <summary>
		/// Subscribes to brightness changed event.
		/// </summary>
		/// <param name="onBrightnessChanged">Action to be invoked when brightness changed</param>
		/// <returns>True if successfully subscribes</returns>
		public bool Subscribe(Action<string, int> onBrightnessChanged)
		{
			_watcher = MSMonitor.StartBrightnessEventWatcher();
			if (_watcher is null)
				return false;

			this._onBrightnessChanged = onBrightnessChanged ?? throw new ArgumentNullException(nameof(onBrightnessChanged));
			_watcher.EventArrived += OnEventArrived;
			return true;
		}

		private void OnEventArrived(object sender, EventArrivedEventArgs e)
		{
			var (instanceName, brightness) = MSMonitor.ParseBrightnessEventArgs(e);
			_onBrightnessChanged?.Invoke(instanceName, brightness);
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
				try
				{
					if (_watcher is not null)
					{
						_watcher.EventArrived -= OnEventArrived;
						_watcher.Stop(); // This may throw InvalidCastException.
						_watcher.Dispose();
					}
				}
				catch (InvalidCastException)
				{
				}
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}