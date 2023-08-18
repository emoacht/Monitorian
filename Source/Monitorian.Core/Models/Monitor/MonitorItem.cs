using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Monitor
{
	internal abstract class MonitorItem : IMonitor, IDisposable
	{
		public string DeviceInstanceId { get; }
		public string Description { get; }
		public byte DisplayIndex { get; }
		public byte MonitorIndex { get; }
		public Rect MonitorRect { get; }
		public bool IsInternal { get; }
		public bool IsReachable { get; }

		public virtual bool IsBrightnessSupported => IsReachable;
		public virtual bool IsContrastSupported => false;
		public virtual bool IsPrecleared => false;

		private Action _onDisposed;

		public MonitorItem(
			string deviceInstanceId,
			string description,
			byte displayIndex,
			byte monitorIndex,
			Rect monitorRect,
			bool isInternal,
			bool isReachable,
			Action onDisposed)
		{
			if (string.IsNullOrWhiteSpace(deviceInstanceId))
				throw new ArgumentNullException(nameof(deviceInstanceId));
			if (string.IsNullOrWhiteSpace(description))
				throw new ArgumentNullException(nameof(description));

			this.DeviceInstanceId = deviceInstanceId;
			this.Description = description;
			this.DisplayIndex = displayIndex;
			this.MonitorIndex = monitorIndex;
			this.MonitorRect = monitorRect;
			this.IsInternal = isInternal;
			this.IsReachable = isReachable;
			this._onDisposed = onDisposed;
		}

		public int Brightness { get; protected set; } = -1;
		public int BrightnessSystemAdjusted { get; protected set; } = -1;

		public abstract AccessResult UpdateBrightness(int brightness = -1);
		public abstract AccessResult SetBrightness(int brightness);

		public int Contrast { get; protected set; } = -1;

		public virtual AccessResult UpdateContrast() => AccessResult.NotSupported;
		public virtual AccessResult SetContrast(int contrast) => AccessResult.NotSupported;

		public virtual (AccessResult result, ValueData data) GetValue(byte code) => (AccessResult.NotSupported, null);
		public virtual (AccessResult result, ValueData data) SetValue(byte code, int value) => (AccessResult.NotSupported, null);

		public override string ToString()
		{
			return SimpleSerialization.Serialize(
				(nameof(Type), this.GetType().Name),
				(nameof(DeviceInstanceId), DeviceInstanceId),
				(nameof(Description), Description),
				(nameof(DisplayIndex), DisplayIndex),
				(nameof(MonitorIndex), MonitorIndex),
				(nameof(MonitorRect), MonitorRect),
				(nameof(IsInternal), IsInternal),
				(nameof(IsReachable), IsReachable),
				(nameof(IsBrightnessSupported), IsBrightnessSupported),
				(nameof(IsContrastSupported), IsContrastSupported),
				(nameof(IsPrecleared), IsPrecleared),
				(nameof(Brightness), Brightness),
				(nameof(BrightnessSystemAdjusted), BrightnessSystemAdjusted),
				(nameof(Contrast), Contrast));
		}

		#region IDisposable

		private bool _isDisposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				_onDisposed?.Invoke();
				_onDisposed = null;
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}