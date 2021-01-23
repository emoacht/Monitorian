using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Monitorian.Core.Models.Watcher
{
	internal abstract class TimerWatcher
	{
		private readonly DispatcherTimer _timer;
		private readonly TimeSpan[] _intervals;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="intervals">Sequence of timer intervals in seconds</param>
		protected TimerWatcher(params int[] intervals)
		{
			if (intervals?.Length is not > 0)
				throw new ArgumentNullException(nameof(intervals));
			if (intervals.Any(x => x <= 0))
				throw new ArgumentOutOfRangeException(nameof(intervals), intervals.First(x => x <= 0), "An interval must be positive.");

			this._intervals = intervals.Select(x => TimeSpan.FromSeconds(x)).ToArray();

			_timer = new DispatcherTimer();
			_timer.Tick += OnTick;
		}

		public int Count { get; private set; }

		public void TimerStart()
		{
			_timer.Stop();
			Count = 0;
			_timer.Interval = _intervals[Count];
			_timer.Start();
		}

		public void TimerStop()
		{
			_timer.Stop();
		}

		private void OnTick(object sender, EventArgs e)
		{
			_timer.Stop();
			Count++;

			TimerTick();

			if (Count < _intervals.Length)
			{
				_timer.Interval = _intervals[Count];
				_timer.Start();
			}
		}

		protected abstract void TimerTick();
	}
}