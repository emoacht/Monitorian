using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Watcher
{
	internal class SessionWatcher : TimerWatcher, IDisposable
	{
		private Action<SessionSwitchCountEventArgs> _onSessionSwitch;

		public SessionWatcher() : base(5, 5)
		{ }

		public void Subscribe(Action<SessionSwitchCountEventArgs> onSessionSwitch)
		{
			this._onSessionSwitch = onSessionSwitch ?? throw new ArgumentNullException(nameof(onSessionSwitch));
			SystemEvents.SessionSwitch += OnSessionSwitch;
		}

		private void RaiseSessionSwitch(SessionSwitchReason reason, int count) =>
			_onSessionSwitch?.Invoke(new SessionSwitchCountEventArgs(reason, count));

		private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
		{
			switch (e.Reason)
			{
				case SessionSwitchReason.SessionLogon:
				case SessionSwitchReason.SessionUnlock:
					RaiseSessionSwitch(e.Reason, 0);
					TimerStart(e);
					break;
				case SessionSwitchReason.SessionLogoff:
				case SessionSwitchReason.SessionLock:
					TimerStop();
					RaiseSessionSwitch(e.Reason, 0);
					break;
			}
		}

		private SessionSwitchReason _reason;

		protected virtual void TimerStart(SessionSwitchEventArgs e)
		{
			this._reason = e.Reason;
			TimerStart();
		}

		protected override void TimerTick() => RaiseSessionSwitch(_reason, Count);

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
				SystemEvents.SessionSwitch -= OnSessionSwitch;
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}

	public class SessionSwitchCountEventArgs : CountEventArgs<SessionSwitchReason>
	{
		public SessionSwitchCountEventArgs(SessionSwitchReason reason, int count) : base(reason, count)
		{ }
	}
}