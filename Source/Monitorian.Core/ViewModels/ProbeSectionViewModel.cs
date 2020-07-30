using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.ViewModels
{
	public class ProbeSectionViewModel : ViewModelBase
	{
		private readonly AppControllerCore _controller;
		public SettingsCore Settings => _controller.Settings;

		public ProbeSectionViewModel(AppControllerCore controller)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
		}

		public bool CanProbe
		{
			get => _canProbe;
			private set => SetPropertyValue(ref _canProbe, value);
		}
		private bool _canProbe = true;

		public void PerformProbe()
		{
			CanProbe = false;

			Task.Run(async () =>
			{
				var log = await MonitorManager.ProbeMonitorsAsync();
				LogService.RecordProbe(log);
			});
		}

		public void PerformCopy()
		{
			Task.Run(() => LogService.CopyOperation());
		}

		public void PerformRescan()
		{
			Task.Run(async () =>
			{
				await _controller.ScanAsync();

				SystemSounds.Asterisk.Play();
			});
		}
	}
}