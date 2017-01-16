using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Monitorian.Models.Watcher
{
	internal class SettingsWatcher : IDisposable
	{
		private readonly DispatcherTimer _timer;
		private Func<Task> _onChanged;

		private int _count = 0;
		private const int _countMax = 3;

		public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(3);

		public SettingsWatcher()
		{
			_timer = new DispatcherTimer();
			_timer.Tick += OnTick;
		}

		public void Subscribe(Func<Task> onChanged)
		{
			if (onChanged == null)
				throw new ArgumentNullException(nameof(onChanged));

			this._onChanged = onChanged;
			SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
		}

		private async void OnDisplaySettingsChanged(object sender, EventArgs e)
		{
			_timer.Stop();

			await _onChanged?.Invoke();

			_count = 0;
			_timer.Interval += Interval;
			_timer.Start();
		}

		private async void OnTick(object sender, EventArgs e)
		{
			_timer.Stop();

			await _onChanged?.Invoke();

			_count++;
			if (_count <= _countMax)
			{
				_timer.Start();
			}
		}

		#region IDisposable

		private bool isDisposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
			}

			// Free any unmanaged objects here.
			isDisposed = true;
		}

		#endregion
	}
}