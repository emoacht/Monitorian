using System;
using System.Media;
using System.Threading.Tasks;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Monitor;

namespace Monitorian.Core.ViewModels;

public class DevSectionViewModel : ViewModelBase
{
	private readonly AppControllerCore _controller;
	public SettingsCore Settings => _controller.Settings;

	public DevSectionViewModel(AppControllerCore controller)
	{
		this._controller = controller ?? throw new ArgumentNullException(nameof(controller));

		Task.Run(async () => Arguments = await _controller.LoadArgumentsAsync());

		PropertyChanged += async (_, e) =>
		{
			switch (e.PropertyName)
			{
				case nameof(Arguments):
					await _controller.SaveArgumentsAsync(Arguments?.Trim());
					break;
			}
		};
	}

	public bool CanProbe
	{
		get => _canProbe;
		private set => SetProperty(ref _canProbe, value);
	}
	private bool _canProbe = true;

	public void PerformProbe()
	{
		CanProbe = false;

		Task.Run(async () =>
		{
			var log = await MonitorManager.ProbeMonitorsAsync();
			Logger.RecordProbe(log);
		});
	}

	public void PerformRescan()
	{
		Task.Run(async () =>
		{
			await _controller.ScanAsync();

			SystemSounds.Asterisk.Play();
		});
	}

	public void PerformCopy()
	{
		Task.Run(() => Logger.CopyOperationAsync());
	}

	public string Arguments
	{
		get => _arguments;
		set => SetProperty(ref _arguments, value);
	}
	private string _arguments;
}