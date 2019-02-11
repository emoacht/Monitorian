using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.ViewModels
{
	public class ProbeViewModel : ViewModelBase
	{
		public ProbeViewModel()
		{ }

		private int _count = 0;
		private const int CountDivider = 3;

		public void EnableProbe()
		{
			if (!CanProbe && (++_count % CountDivider == 0))
				CanProbe = true;
		}

		public bool CanProbe
		{
			get => _canProbe;
			private set => SetPropertyValue(ref _canProbe, value);
		}
		private bool _canProbe;

		public void PerformProbe()
		{
			CanProbe = false;

			Task.Run(async () =>
			{
				var log = await MonitorManager.ProbeMonitorsAsync();
				LogService.RecordProbe(log);
			});
		}
	}
}