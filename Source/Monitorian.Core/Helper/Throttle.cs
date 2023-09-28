using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Monitorian.Core.Helper;

/// <summary>
/// Rx Throttle like operator
/// </summary>
public class Throttle
{
	protected readonly SemaphoreSlim _semaphore = new(1, 1);
	protected readonly DispatcherTimer _timer;
	protected readonly Action _action;

	public Throttle(TimeSpan dueTime, Action action)
	{
		if (dueTime <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime, "The time must be positive.");

		_timer = new(
			interval: dueTime,
			priority: DispatcherPriority.Background,
			callback: OnTick,
			dispatcher: Application.Current.Dispatcher);

		this._action = action;
	}

	private async void OnTick(object sender, EventArgs e)
	{
		try
		{
			await _semaphore.WaitAsync();

			if (!_timer.IsEnabled)
				return;

			_timer.Stop();
			_action?.Invoke();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public virtual async Task PushAsync()
	{
		try
		{
			await _semaphore.WaitAsync();

			_timer.Stop();
			_timer.Start();
		}
		finally
		{
			_semaphore.Release();
		}
	}
}

/// <summary>
/// Rx Throttle like operator which holds generic objects in a queue
/// </summary>
public class Throttle<T>
{
	protected readonly SemaphoreSlim _semaphore = new(1, 1);
	protected readonly DispatcherTimer _timer;
	protected readonly Queue<T> _queue = new();
	protected readonly Action<T[]> _action;

	public Throttle(TimeSpan dueTime, Action<T[]> action)
	{
		if (dueTime <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime, "The time must be positive.");

		_timer = new(
			interval: dueTime,
			priority: DispatcherPriority.Background,
			callback: OnTick,
			dispatcher: Application.Current.Dispatcher);

		this._action = action;
	}

	private async void OnTick(object sender, EventArgs e)
	{
		try
		{
			await _semaphore.WaitAsync();

			if (!_timer.IsEnabled)
				return;

			_timer.Stop();
			_action?.Invoke(_queue.ToArray());
			_queue.Clear();

		}
		finally
		{
			_semaphore.Release();
		}
	}

	public virtual async Task PushAsync(T item)
	{
		try
		{
			await _semaphore.WaitAsync();

			_timer.Stop();
			_queue.Enqueue(item);
			_timer.Start();
		}
		finally
		{
			_semaphore.Release();
		}
	}
}

/// <summary>
/// Rx Sample like operator
/// </summary>
public class Sample : Throttle
{
	public Sample(TimeSpan dueTime, Action action) : base(dueTime, action)
	{ }

	public override async Task PushAsync()
	{
		try
		{
			await _semaphore.WaitAsync();

			if (!_timer.IsEnabled)
				_timer.Start();
		}
		finally
		{
			_semaphore.Release();
		}
	}
}