using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Models.Watcher
{
	internal class PowerWatcher : IDisposable
	{
		private Func<Task> _onChanged;

		public PowerWatcher()
		{
		}

		public void Subscribe(Func<Task> onChanged)
		{
			if (onChanged == null)
				throw new ArgumentNullException(nameof(onChanged));

			this._onChanged = onChanged;
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		private async void OnPowerModeChanged(object sender, EventArgs e)
		{
			await _onChanged?.Invoke();
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