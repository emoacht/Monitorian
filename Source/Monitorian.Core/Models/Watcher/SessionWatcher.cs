using System;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Watcher;

internal class SessionWatcher : TimerWatcher
{
	private Action<ICountEventArgs> _onSessionSwitch;

	public SessionWatcher() : base(5, 5)
	{ }

	public void Subscribe(Action<ICountEventArgs> onSessionSwitch)
	{
		this._onSessionSwitch = onSessionSwitch ?? throw new ArgumentNullException(nameof(onSessionSwitch));
		SystemEvents.SessionSwitch += OnSessionSwitch;
	}

	public bool IsLocked { get; private set; }

	private void RaiseSessionSwitch(SessionSwitchReason reason, int count) =>
		_onSessionSwitch?.Invoke(new SessionSwitchCountEventArgs(reason, count));

	private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
	{
		switch (e.Reason)
		{
			case SessionSwitchReason.SessionLogon:
			case SessionSwitchReason.SessionUnlock:
				IsLocked = false;
				RaiseSessionSwitch(e.Reason, 0);
				TimerStart(e);
				break;
			case SessionSwitchReason.SessionLogoff:
			case SessionSwitchReason.SessionLock:
				TimerStop();
				IsLocked = true;
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

	protected override void Dispose(bool disposing)
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

		base.Dispose(disposing);
	}

	#endregion
}

public class SessionSwitchCountEventArgs : CountEventArgs<SessionSwitchReason>
{
	public SessionSwitchCountEventArgs(SessionSwitchReason reason, int count) : base(reason, count)
	{ }
}