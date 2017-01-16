using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Models.Watcher
{
	internal class PowerChangeWatcher
	{
		private Func<Task> _action;

		public PowerChangeWatcher()
		{
		}

		public void Start(Func<Task> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_action = action;
			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		public void Stop()
		{
			SystemEvents.DisplaySettingsChanged -= OnPowerModeChanged;
		}

		private async void OnPowerModeChanged(object sender, EventArgs e)
		{
			await _action?.Invoke();
		}
	}
}