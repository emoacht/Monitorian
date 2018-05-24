using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
{
	internal class InaccessibleMonitorItem : MonitorItem
	{
		public InaccessibleMonitorItem(
			string deviceInstanceId,
			string description,
			byte displayIndex,
			byte monitorIndex) : base(
				deviceInstanceId: deviceInstanceId,
				description: description,
				displayIndex: displayIndex,
				monitorIndex: monitorIndex,
				isAccessible: false)
		{ }

		public override bool UpdateBrightness(int brightness = -1) => false;
		public override bool SetBrightness(int brightness) => false;
	}
}