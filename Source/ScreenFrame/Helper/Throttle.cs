using System;
using System.Threading.Tasks;

namespace ScreenFrame.Helper;

/// <summary>
/// Rx Throttle like operator
/// </summary>
internal class Throttle
{
	private static readonly TimeSpan _dueTime = TimeSpan.FromSeconds(0.2);
	private readonly Action _action;

	public Throttle(Action action) => this._action = action;

	private Task _lastWaitTask;

	public async Task PushAsync()
	{
		var currentWaitTask = Task.Delay(_dueTime);
		_lastWaitTask = currentWaitTask;
		await currentWaitTask;
		if (_lastWaitTask == currentWaitTask)
		{
			_action?.Invoke();
		}
	}
}

internal class Throttle<T>
{
	private static readonly TimeSpan _dueTime = TimeSpan.FromSeconds(0.2);
	private readonly Action<T> _action;

	public Throttle(Action<T> action) => this._action = action;

	private Task _lastWaitTask;

	public async Task PushAsync(T value)
	{
		var currentWaitTask = Task.Delay(_dueTime);
		_lastWaitTask = currentWaitTask;
		await currentWaitTask;
		if (_lastWaitTask == currentWaitTask)
		{
			_action?.Invoke(value);
		}
	}
}