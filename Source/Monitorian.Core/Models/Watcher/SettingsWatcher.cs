using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Watcher
{
	internal class SettingsWatcher : TimerWatcher, IDisposable
	{
		private Action _onChanged;

		public SettingsWatcher() : base(3, 3, 6)
		{ }

		public void Subscribe(Action onChanged)
		{
			this._onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
			SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
		}

		private void OnDisplaySettingsChanged(object sender, EventArgs e)
		{
			TimerStop();

			_onChanged?.Invoke();

			TimerStart();
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
				SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}