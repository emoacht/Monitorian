using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
{
	public interface IMonitor : IDisposable
	{
		string DeviceInstanceId { get; }
		string Description { get; }
		byte DisplayIndex { get; }
		byte MonitorIndex { get; }
		bool IsAccessible { get; }

		int Brightness { get; }
		int BrightnessAdjusted { get; }

		bool UpdateBrightness(int brightness = -1);
		bool SetBrightness(int brightness);
	}
}