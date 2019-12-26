using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor
{
	public interface IMonitor : IDisposable
	{
		string DeviceInstanceId { get; }
		string Description { get; }
		byte DisplayIndex { get; }
		byte MonitorIndex { get; }
		bool IsReachable { get; }

		int Brightness { get; }
		int BrightnessSystemAdjusted { get; }

		bool UpdateBrightness(int brightness = -1);
		bool SetBrightness(int brightness);
	}
}