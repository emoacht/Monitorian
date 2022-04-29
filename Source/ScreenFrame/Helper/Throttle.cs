using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenFrame.Helper
{
	/// <summary>
	/// Rx Throttle like operator
	/// </summary>
	internal class Throttle
	{
		private static readonly TimeSpan _dueTime = TimeSpan.FromSeconds(1);
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
}