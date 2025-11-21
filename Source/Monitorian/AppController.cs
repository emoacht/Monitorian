using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Monitorian.Core;
using Monitorian.Core.Models.Monitor;
using Monitorian.Models;

namespace Monitorian;

public class AppController : AppControllerCore
{
	public new Settings Settings => (Settings)base.Settings;

	public AppController(AppKeeper keeper) : base(keeper, new Settings())
	{
	}

	protected override async Task<IEnumerable<IMonitor>> EnumerateCustomMonitorsAsync()
	{
		var customMonitors = new List<IMonitor>();

		// Add Cisco Webex device if enabled and configured
		if (Settings.EnablesWebex &&
			!string.IsNullOrWhiteSpace(Settings.WebexHost) &&
			!string.IsNullOrWhiteSpace(Settings.WebexUsername) &&
			!string.IsNullOrWhiteSpace(Settings.WebexPassword))
		{
			try
			{
				var webexMonitor = new WebexMonitorItem(
					deviceInstanceId: $"WEBEX\\{Settings.WebexHost}",
					description: $"Cisco Webex Desk ({Settings.WebexHost})",
					host: Settings.WebexHost,
					port: Settings.WebexPort,
					username: Settings.WebexUsername,
					password: Settings.WebexPassword);

				customMonitors.Add(webexMonitor);
			}
			catch
			{
				// Failed to create Webex monitor - silently ignore
			}
		}

		return await Task.FromResult(customMonitors);
	}
}