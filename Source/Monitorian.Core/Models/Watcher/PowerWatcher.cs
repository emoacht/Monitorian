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
		private Action _onPowerModeChanged;
		private Action<PowerSettingChangedEventArgs> _onPowerSettingChanged;

		private SystemEventsComplement _complement;

		public PowerWatcher() : base(5, 5, 10, 20, 40, 80)
		{ }

		public void Subscribe(Action onPowerModeChanged)
		{
			this._onPowerModeChanged = onPowerModeChanged ?? throw new ArgumentNullException(nameof(onPowerModeChanged));
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		public void Subscribe(Action onPowerStatusChanged, (Guid[] guids, Action<PowerSettingChangedEventArgs> action) onPowerSettingChanged)
		{
			Subscribe(onPowerStatusChanged);

			this._onPowerSettingChanged = onPowerSettingChanged.action;
			_complement = new SystemEventsComplement();
			_complement.PowerSettingChanged += (sender, e) => this._onPowerSettingChanged.Invoke(e);
			_complement.RegisterPowerSettingEvent(onPowerSettingChanged.guids);
		}

		private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			TimerStop();

			switch (e.Mode)
			{
				default:
					_onPowerModeChanged?.Invoke();
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
			_onPowerModeChanged?.Invoke();
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
				_complement?.UnregisterPowerSettingEvent();
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}