using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Watcher
{
	internal class PowerWatcher : IDisposable
	{
		private Action _onPowerModeChanged;
		private Action<PowerSettingChangedEventArgs> _onPowerSettingChanged;

		private SystemEventsComplement _complement;

		private class PowerModeWatcher : TimerWatcher
		{
			private readonly PowerWatcher _instance;

			public PowerModeWatcher(PowerWatcher instance, params int[] intervals) : base(intervals) => this._instance = instance;

			public new void TimerStart() => base.TimerStart();
			public new void TimerStop() => base.TimerStop();

			protected override void TimerTick() => _instance._onPowerModeChanged?.Invoke();
		}

		private readonly PowerModeWatcher _resumeWatcher;
		private readonly PowerModeWatcher _statusWatcher;

		public PowerWatcher()
		{
			// Conform invocation timings so that the action would be executed efficiently when
			// multiple and different events are fired almost simultaneously.
			_resumeWatcher = new PowerModeWatcher(this, 5, 5, 10, 10, 30);
			_statusWatcher = new PowerModeWatcher(this, 1, 4);
		}

		public void Subscribe(Action onPowerModeChanged)
		{
			this._onPowerModeChanged = onPowerModeChanged ?? throw new ArgumentNullException(nameof(onPowerModeChanged));
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		public void Subscribe(Action onPowerStatusChanged, (IReadOnlyCollection<Guid> guids, Action<PowerSettingChangedEventArgs> action) onPowerSettingChanged)
		{
			Subscribe(onPowerStatusChanged);

			if (onPowerSettingChanged.action is null)
				return;

			this._onPowerSettingChanged = onPowerSettingChanged.action;
			_complement = new SystemEventsComplement();
			_complement.PowerSettingChanged += (sender, e) => this._onPowerSettingChanged.Invoke(e);
			_complement.RegisterPowerSettingEvent(onPowerSettingChanged.guids);
		}

		private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			switch (e.Mode)
			{
				case PowerModes.Resume:
					_resumeWatcher.TimerStart();
					break;
				case PowerModes.Suspend:
					_resumeWatcher.TimerStop();
					break;
				default:
					_statusWatcher.TimerStart();
					break;
			}
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