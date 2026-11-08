using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace Monitorian.Core.Helper;

/// <summary>
/// Rx Throttle like operator
/// </summary>
public class Throttle
{
	protected readonly DispatcherTimer _timer;
	protected readonly Action _action;

	public Throttle(TimeSpan dueTime, Action action) : this(dueTime, action, Application.Current.Dispatcher)
	{ }

	public Throttle(TimeSpan dueTime, Action action, Dispatcher dispatcher)
	{
		if (dueTime <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime, "The time must be positive.");

		this._action = action ?? throw new ArgumentNullException(nameof(action));

		_timer = new(DispatcherPriority.Background, dispatcher: dispatcher) { Interval = dueTime };
		_timer.Tick += OnTick;
	}

	public void Push()
	{
		if (_timer.Dispatcher.CheckAccess())
		{
			Restart();
		}
		else
		{
			_timer.Dispatcher.Invoke(Restart);
		}
	}

	protected virtual void Restart()
	{
		_timer.Stop();
		_timer.Start();
	}

	private void OnTick(object sender, EventArgs e)
	{
		_timer.Stop();
		_action.Invoke();
	}
}

/// <summary>
/// Rx Throttle like operator which holds generic objects in a queue
/// </summary>
public class Throttle<T>
{
	protected readonly DispatcherTimer _timer;
	protected readonly Action<T[]> _action;
	protected readonly Queue<T> _queue = [];

	public Throttle(TimeSpan dueTime, Action<T[]> action) : this(dueTime, action, Application.Current.Dispatcher)
	{ }

	public Throttle(TimeSpan dueTime, Action<T[]> action, Dispatcher dispatcher)
	{
		if (dueTime <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime, "The time must be positive.");

		this._action = action ?? throw new ArgumentNullException(nameof(action));

		_timer = new(DispatcherPriority.Background, dispatcher: dispatcher) { Interval = dueTime };
		_timer.Tick += OnTick;
	}

	public void Push(T item)
	{
		if (_timer.Dispatcher.CheckAccess())
		{
			Restart(item);
		}
		else
		{
			_timer.Dispatcher.Invoke(() => Restart(item));
		}
	}

	protected virtual void Restart(T item)
	{
		_timer.Stop();
		_queue.Enqueue(item);
		_timer.Start();
	}

	private void OnTick(object sender, EventArgs e)
	{
		_timer.Stop();
		var buffer = _queue.ToArray();
		_queue.Clear();
		_action.Invoke(buffer);
	}
}

/// <summary>
/// Rx Sample like operator
/// </summary>
public class Sample : Throttle
{
	public Sample(TimeSpan dueTime, Action action) : base(dueTime, action)
	{ }

	public Sample(TimeSpan dueTime, Action action, Dispatcher dispatcher) : base(dueTime, action, dispatcher)
	{ }

	protected override void Restart()
	{
		if (!_timer.IsEnabled)
			_timer.Start();
	}
}