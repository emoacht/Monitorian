using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Models.Monitor
{
	/// <summary>
	/// Physical monitor controlled by DDC/CI (external monitor)
	/// </summary>
	internal class DdcMonitorItem : MonitorItem, IMonitor
	{
		public SafePhysicalMonitorHandle Handle { get; }

		public DdcMonitorItem(
			string description,
			string deviceInstanceId,
			byte displayIndex,
			byte monitorIndex,
			SafePhysicalMonitorHandle handle) : base(
				description,
				deviceInstanceId,
				displayIndex,
				monitorIndex)
		{
			if (handle == null)
				throw new ArgumentNullException(nameof(handle));

			this.Handle = handle;
		}

		private readonly object _lock = new object();

		public bool UpdateBrightness(int brightness = -1)
		{
			lock (_lock)
			{
				this.Brightness = MonitorConfiguration.GetBrightness(Handle);
				return (0 <= this.Brightness);
			}
		}

		public bool SetBrightness(int brightness)
		{
			if ((brightness < 0) || (100 < brightness))
				throw new ArgumentOutOfRangeException(nameof(brightness));

			lock (_lock)
			{
				if (MonitorConfiguration.SetBrightness(Handle, brightness))
				{
					this.Brightness = brightness;
					return true;
				}
				return false;
			}
		}

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
		{
			lock (_lock)
			{
				if (_isDisposed)
					return;

				if (disposing)
				{
					// Free any other managed objects here.
					Handle.Dispose();
				}

				// Free any unmanaged objects here.
				_isDisposed = true;

				base.Dispose(disposing);
			}
		}

		#endregion
	}

	internal class SafePhysicalMonitorHandle : SafeHandle
	{
		public SafePhysicalMonitorHandle(IntPtr handle) : base(IntPtr.Zero, true)
		{
			this.handle = handle; // A valid handle may be IntPtr.Zero.
		}

		public override bool IsInvalid => false; // The validity cannot be checked by the handle.

		protected override bool ReleaseHandle()
		{
			return MonitorConfiguration.DestroyPhysicalMonitor(handle);
		}
	}
}