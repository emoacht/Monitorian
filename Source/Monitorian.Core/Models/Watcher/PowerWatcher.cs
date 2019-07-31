using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Watcher
{
	internal class PowerWatcher : TimerWatcher, IDisposable
	{
		private Action _onChanged;

		public PowerWatcher() : base(5, 5, 10, 20, 40, 80)
		{ }

		public void Subscribe(Action onChanged)
		{
			this._onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			TimerStop();

			switch (e.Mode)
			{
				default:
					_onChanged?.Invoke();
					break;
				case PowerModes.Suspend:
					// Do nothing.
					break;
				case PowerModes.Resume:
					TimerStart();
					break;
			}
		}

		protected override void TimerTick()
		{
			_onChanged?.Invoke();
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