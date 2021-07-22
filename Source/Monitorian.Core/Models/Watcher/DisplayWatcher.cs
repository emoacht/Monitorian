using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Watcher
{
	internal class DisplayWatcher : TimerWatcher, IDisposable
	{
		private Action _onDisplaySettingsChanged;

		public DisplayWatcher() : base(1, 4, 5)
		{ }

		public void Subscribe(Action onDisplaySettingsChanged)
		{
			this._onDisplaySettingsChanged = onDisplaySettingsChanged ?? throw new ArgumentNullException(nameof(onDisplaySettingsChanged));
			SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
		}

		public void RaiseDisplaySettingsChanged() => OnDisplaySettingsChanged(this, EventArgs.Empty);

		private void OnDisplaySettingsChanged(object sender, EventArgs e)
		{
			TimerStart();
		}

		protected override void TimerTick() => _onDisplaySettingsChanged?.Invoke();

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