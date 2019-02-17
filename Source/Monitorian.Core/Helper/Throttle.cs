using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Monitorian.Core.Helper
{
	/// <summary>
	/// Rx Throttle like operator
	/// </summary>
	public class Throttle
	{
		protected readonly Action _action;
		protected readonly DispatcherTimer _timer;
		protected readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		public Throttle(TimeSpan dueTime, Action action)
		{
			if (dueTime <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(dueTime));

			this._action = action;

			_timer = new DispatcherTimer { Interval = dueTime };
			_timer.Tick += OnTick;
		}

		private void OnTick(object sender, EventArgs e)
		{
			try
			{
				_semaphore.Wait();

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
}