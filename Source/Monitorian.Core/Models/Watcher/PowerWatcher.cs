using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.Models.Watcher
{
	internal class PowerWatcher : IDisposable
	{
		private Action<ICountEventArgs> _onPowerChanged;

		private void RaisePowerModeChanged(PowerModes mode, int count) =>
			_onPowerChanged?.Invoke(new PowerModeChangedCountEventArgs(mode, count));

		private void RaiseDisplayStateChanged(DisplayStates state, int count) =>
			_onPowerChanged?.Invoke(new DisplayStateChangedCountEventArgs(state, count));

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
		private readonly PowerDataWatcher<DisplayStates> _stateWatcher;

		public PowerWatcher()
		{
			// Conform invocation timings so that the action would be executed efficiently when
			// multiple and different events are fired almost simultaneously.
			_resumeWatcher = new PowerDataWatcher<PowerModes>(this.RaisePowerModeChanged, 5, 5, 10, 10, 30);
			_statusWatcher = new PowerDataWatcher<PowerModes>(this.RaisePowerModeChanged, 1, 4);
			_stateWatcher = new PowerDataWatcher<DisplayStates>(this.RaiseDisplayStateChanged, 5, 5);
		}

		public void Subscribe(Action<ICountEventArgs> onPowerChanged)
		{
			this._onPowerChanged = onPowerChanged ?? throw new ArgumentNullException(nameof(onPowerChanged));
			SystemEvents.PowerModeChanged += OnPowerModeChanged;

			var (powerSettingGuids, powerSettingChanged) = PowerManagement.GetOnPowerSettingChanged();

			_complement = new SystemEventsComplement();
			_complement.PowerSettingChanged += (_, e) => OnDisplayStateChanged(powerSettingChanged.Invoke(e));
			_complement.RegisterPowerSettingEvent(powerSettingGuids);
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

		private void OnDisplayStateChanged(DisplayStates state)
		{
			switch (state)
			{
				case DisplayStates.On:
				case DisplayStates.Dimmed:
					RaiseDisplayStateChanged(state, 0);
					_stateWatcher.TimerStart(state);
					break;
				case DisplayStates.Off:
					_stateWatcher.TimerStop();
					RaiseDisplayStateChanged(state, 0);
					break;
			};
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
				_stateWatcher.Dispose();
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

	public class DisplayStateChangedCountEventArgs : CountEventArgs<DisplayStates>
	{
		public DisplayStateChangedCountEventArgs(DisplayStates state, int count) : base(state, count)
		{ }
	}
}