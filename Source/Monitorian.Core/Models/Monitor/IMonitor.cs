using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Models.Monitor
{
	public interface IMonitor : IDisposable
	{
		string DeviceInstanceId { get; }
		string Description { get; }
		byte DisplayIndex { get; }
		byte MonitorIndex { get; }
		Rect MonitorRect { get; }
		bool IsReachable { get; }

		int Brightness { get; }
		int BrightnessSystemAdjusted { get; }

		AccessResult UpdateBrightness(int brightness = -1);
		AccessResult SetBrightness(int brightness);
	}

	public enum AccessStatus
	{
		None = 0,
		Succeeded,
		Failed,
		DdcFailed,
		NoLongerExist
	}

	public class AccessResult
	{
		public AccessStatus Status { get; }
		public string Message { get; }

		public AccessResult(AccessStatus status, string message) => (this.Status, this.Message) = (status, message);

		public static readonly AccessResult Succeeded = new(AccessStatus.Succeeded, null);
		public static readonly AccessResult Failed = new(AccessStatus.Failed, null);
	}
}