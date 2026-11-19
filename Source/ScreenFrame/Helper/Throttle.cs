using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenFrame.Helper;

/// <summary>
/// Rx Throttle like operator
/// </summary>
internal class Throttle
{
	protected readonly TimeSpan _dueTime;
	protected readonly Action _action;

	public Throttle(TimeSpan dueTime, Action action)
	{
		if (dueTime <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime, "The time must be positive.");

		this._dueTime = dueTime;
		this._action = action ?? throw new ArgumentNullException(nameof(action));
	}

	private Task _lastWaitTask;

	public virtual async Task PushAsync()
	{
		var currentWaitTask = Task.Delay(_dueTime);
		Volatile.Write(ref _lastWaitTask, currentWaitTask);
		await currentWaitTask;
		if (Volatile.Read(ref _lastWaitTask) == currentWaitTask)
		{
			_action.Invoke();
		}
	}
}

internal class Throttle<T>
{
	protected readonly TimeSpan _dueTime;
	protected readonly Action<T> _action;

	public Throttle(TimeSpan dueTime, Action<T> action)
	{
		if (dueTime <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime, "The time must be positive.");

		this._dueTime = dueTime;
		this._action = action ?? throw new ArgumentNullException(nameof(action));
	}

	private Task _lastWaitTask;

	public virtual async Task PushAsync(T value)
	{
		var currentWaitTask = Task.Delay(_dueTime);
		Volatile.Write(ref _lastWaitTask, currentWaitTask);
		await currentWaitTask;
		if (Volatile.Read(ref _lastWaitTask) == currentWaitTask)
		{
			_action.Invoke(value);
		}
	}
}