using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Monitorian.Models.Watcher
{
	internal abstract class TimerWatcher
	{
		private readonly DispatcherTimer _timer;

		protected TimeSpan TimerInterval { get; set; } = TimeSpan.FromSeconds(3);

		private int _count = 0;
		private readonly int _countLimit = 1;

		protected TimerWatcher(int countLimit = 1)
		{
			if (countLimit <= 0)
				throw new ArgumentOutOfRangeException(nameof(countLimit), "The count must be greater than 0.");

			this._countLimit = countLimit;

			_timer = new DispatcherTimer();
			_timer.Tick += OnTick;
		}

		protected void TimerReset()
		{
			_timer.Stop();
			_count = 0;
		}

		protected void TimerStart()
		{
			_timer.Interval += TimerInterval;
			_timer.Start();
		}

		private async void OnTick(object sender, EventArgs e)
		{
			_timer.Stop();

			await TimerTick();

			_count++;
			if (_count < _countLimit)
			{
				_timer.Start();
			}
		}

		protected abstract Task TimerTick();
	}
}