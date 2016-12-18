using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Monitorian.Models.Watcher
{
	internal class SettingsChangeWatcher
	{
		private readonly DispatcherTimer _timer;
		private Func<Task> _action;

		private int _count = 0;
		private const int _countMax = 3;

		public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(3);

		public SettingsChangeWatcher()
		{
			_timer = new DispatcherTimer();
			_timer.Tick += OnTick;
		}

		public void Start(Func<Task> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_action = action;
			SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
		}

		public void Stop()
		{
			SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
		}

		private async void OnDisplaySettingsChanged(object sender, EventArgs e)
		{
			_timer.Stop();

			await _action?.Invoke();

			_count = 0;
			_timer.Interval += Interval;
			_timer.Start();
		}

		private async void OnTick(object sender, EventArgs e)
		{
			_timer.Stop();

			await _action?.Invoke();

			_count++;
			if (_count <= _countMax)
			{
				_timer.Start();
			}
		}
	}
}
