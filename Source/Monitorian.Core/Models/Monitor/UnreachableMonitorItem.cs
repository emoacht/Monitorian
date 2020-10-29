using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor
{
	internal class UnreachableMonitorItem : MonitorItem
	{
		public bool IsInternal { get; }

		public UnreachableMonitorItem(
			string deviceInstanceId,
			string description,
			byte displayIndex,
			byte monitorIndex,
			bool isInternal) : base(
				deviceInstanceId: deviceInstanceId,
				description: description,
				displayIndex: displayIndex,
				monitorIndex: monitorIndex,
				isReachable: false)
		{
			this.IsInternal = isInternal;
		}

		public override bool UpdateBrightness(int brightness = -1) => false;
		public override bool SetBrightness(int brightness) => false;
	}
}