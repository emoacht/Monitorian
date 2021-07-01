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
		private Action<PowerModeChangedCountEventArgs> _onPowerModeChanged;
		private Action<PowerSettingChangedEventArgs> _onPowerSettingChanged;

		private void RaisePowerModeChanged(PowerModes mode, int count) =>
			_onPowerModeChanged?.Invoke(new PowerModeChangedCountEventArgs(mode, count));

		private SystemEventsComplement _complement;

		private class PowerModeWatcher : TimerWatcher
		{
			private readonly PowerWatcher _instance;

			public PowerModeWatcher(PowerWatcher instance, params int[] intervals) : base(intervals) => this._instance = instance;

			public PowerModes Mode { get; private set; }

			public void TimerStart(PowerModeChangedEventArgs e)
			{
				this.Mode = e.Mode;
				base.TimerStart();
			}

			protected override void TimerTick() => _instance.RaisePowerModeChanged(Mode, Count);
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

		public void Subscribe(Action<PowerModeChangedCountEventArgs> onPowerModeChanged)
		{
			this._onPowerModeChanged = onPowerModeChanged ?? throw new ArgumentNullException(nameof(onPowerModeChanged));
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		public void Subscribe(Action<PowerModeChangedCountEventArgs> onPowerModeChanged, (IReadOnlyCollection<Guid> guids, Action<PowerSettingChangedEventArgs> action) onPowerSettingChanged)
		{
			Subscribe(onPowerModeChanged);

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
					RaisePowerModeChanged(PowerModes.Resume, 0);
					_resumeWatcher.TimerStart(e);
					break;
				case PowerModes.Suspend:
					_resumeWatcher.TimerStop();
					RaisePowerModeChanged(PowerModes.Suspend, 0);
					break;
				default:
					_statusWatcher.TimerStart(e);
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

	public class PowerModeChangedCountEventArgs : CountEventArgs<PowerModes>
	{
		public PowerModeChangedCountEventArgs(PowerModes mode, int count) : base(mode, count)
		{ }
	}
}