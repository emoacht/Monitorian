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
		private Action<ICountEventArgs> _onPowerModeChanged;
		private Action<PowerSettingChangedEventArgs> _onPowerSettingChanged;

		private void RaisePowerModeChanged(PowerModes mode, int count) =>
			_onPowerModeChanged?.Invoke(new PowerModeChangedCountEventArgs(mode, count));

		private SystemEventsComplement _complement;

		private class PowerDataWatcher<T> : TimerWatcher
		{
			private readonly Action<T, int> _action;

			public PowerDataWatcher(Action<T, int> action, params int[] intervals) : base(intervals) => this._action = action;

			public T Data { get; private set; }

			public void TimerStart(T data)
			{
				this.Data = data;
				base.TimerStart();
			}

			protected override void TimerTick() => _action.Invoke(Data, Count);
		}

		private readonly PowerDataWatcher<PowerModes> _resumeWatcher;
		private readonly PowerDataWatcher<PowerModes> _statusWatcher;

		public PowerWatcher()
		{
			// Conform invocation timings so that the action would be executed efficiently when
			// multiple and different events are fired almost simultaneously.
			_resumeWatcher = new PowerDataWatcher<PowerModes>(this.RaisePowerModeChanged, 5, 5, 10, 10, 30);
			_statusWatcher = new PowerDataWatcher<PowerModes>(this.RaisePowerModeChanged, 1, 4);
		}

		public void Subscribe(Action<ICountEventArgs> onPowerModeChanged)
		{
			this._onPowerModeChanged = onPowerModeChanged ?? throw new ArgumentNullException(nameof(onPowerModeChanged));
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		public void Subscribe(Action<ICountEventArgs> onPowerModeChanged, (IReadOnlyCollection<Guid> guids, Action<PowerSettingChangedEventArgs> action) onPowerSettingChanged)
		{
			Subscribe(onPowerModeChanged);

			if (onPowerSettingChanged.action is null)
				return;

			this._onPowerSettingChanged = onPowerSettingChanged.action;
			_complement = new SystemEventsComplement();
			_complement.PowerSettingChanged += (_, e) => this._onPowerSettingChanged.Invoke(e);
			_complement.RegisterPowerSettingEvent(onPowerSettingChanged.guids);
		}

		private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			switch (e.Mode)
			{
				case PowerModes.Resume:
					RaisePowerModeChanged(e.Mode, 0);
					_resumeWatcher.TimerStart(e.Mode);
					break;
				case PowerModes.Suspend:
					_resumeWatcher.TimerStop();
					RaisePowerModeChanged(e.Mode, 0);
					break;
				default:
					_statusWatcher.TimerStart(e.Mode);
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
				_resumeWatcher.Dispose();
				_statusWatcher.Dispose();
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