using System;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor;

internal interface IMonitorSource : IDisposable
{
	string DeviceInstanceId { get; }

	Task<IMonitor> GetMonitorAsync(TimeSpan timeout);
}