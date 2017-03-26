using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Models.Watcher
{
	internal class PowerWatcher : TimerWatcher, IDisposable
	{
		private Func<Task> _onChanged;

		public PowerWatcher() : base(countLimit: 2)
		{
		}

		public void Subscribe(Func<Task> onChanged)
		{
			this._onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			TimerReset();

			switch (e.Mode)
			{
				default:
					await _onChanged?.Invoke();
					break;
				case PowerModes.Suspend:
					// Do nothing.
					break;
				case PowerModes.Resume:
					TimerStart();
					break;
			}
		}

		protected override Task TimerTick()
		{
			return _onChanged?.Invoke();
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
				SystemEvents.PowerModeChanged -= OnPowerModeChanged;
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}