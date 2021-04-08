using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Models.Monitor
{
	/// <summary>
	/// Physical monitor controlled by DDC/CI (external monitor)
	/// </summary>
	internal class DdcMonitorItem : MonitorItem
	{
		private readonly SafePhysicalMonitorHandle _handle;
		private readonly bool _useHighLevel;

		public DdcMonitorItem(
			string deviceInstanceId,
			string description,
			byte displayIndex,
			byte monitorIndex,
			Rect monitorRect,
			SafePhysicalMonitorHandle handle,
			bool useHighLevel = true) : base(
				deviceInstanceId: deviceInstanceId,
				description: description,
				displayIndex: displayIndex,
				monitorIndex: monitorIndex,
				monitorRect: monitorRect,
				isReachable: true)
		{
			this._handle = handle ?? throw new ArgumentNullException(nameof(handle));
			this._useHighLevel = useHighLevel;
		}

		private uint _minimum = 0; // Raw minimum brightness (not always 0)
		private uint _maximum = 100; // Raw maximum brightness (not always 100)

		public override AccessResult UpdateBrightness(int brightness = -1)
		{
			var (result, minimum, current, maximum) = MonitorConfiguration.GetBrightness(_handle, _useHighLevel);

			if ((result.Status == AccessStatus.Succeeded) && (minimum < maximum) && (minimum <= current) && (current <= maximum))
			{
				this.Brightness = (int)Math.Round((double)(current - minimum) / (maximum - minimum) * 100D, MidpointRounding.AwayFromZero);
				this._minimum = minimum;
				this._maximum = maximum;
			}
			else
			{
				this.Brightness = -1; // Default
			}
			return result;
		}

		public override AccessResult SetBrightness(int brightness)
		{
			if (brightness is < 0 or > 100)
				throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be within 0 to 100.");

			var buffer = (uint)Math.Round(brightness / 100D * (_maximum - _minimum) + _minimum, MidpointRounding.AwayFromZero);

			var result = MonitorConfiguration.SetBrightness(_handle, buffer, _useHighLevel);

			if (result.Status == AccessStatus.Succeeded)
			{
				this.Brightness = brightness;
			}
			return result;
		}

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				_handle.Dispose();
			}

			// Free any unmanaged objects here.
			_isDisposed = true;

			base.Dispose(disposing);
		}

		#endregion
	}

	internal class SafePhysicalMonitorHandle : SafeHandle
	{
		public SafePhysicalMonitorHandle(IntPtr handle) : base(IntPtr.Zero, true)
		{
			this.handle = handle; // IntPtr.Zero may be a valid handle.
		}

		public override bool IsInvalid => false; // The validity cannot be checked by the handle.

		protected override bool ReleaseHandle()
		{
			return MonitorConfiguration.DestroyPhysicalMonitor(handle);
		}
	}
}