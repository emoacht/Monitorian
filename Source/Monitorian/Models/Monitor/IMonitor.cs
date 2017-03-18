using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
{
	public interface IMonitor : IDisposable
	{
		string Description { get; }
		string DeviceInstanceId { get; }
		byte DisplayIndex { get; }
		byte MonitorIndex { get; }

		int Brightness { get; }
		int BrightnessAdjusted { get; }

		bool UpdateBrightness(int brightness = -1);
		bool SetBrightness(int brightness);
	}
}