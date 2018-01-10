using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Monitorian.Helper
{
	internal class Throttle
	{
		private readonly Action _action;
		private readonly DispatcherTimer _timer;
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

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

		public void Invoke()
		{
			try
			{
				_semaphore.Wait();

				_timer.Stop();
				_timer.Start();
			}
			finally
			{
				_semaphore.Release();
			}
		}
	}
}