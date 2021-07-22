using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Monitor
{
	/// <summary>
	/// Physical monitor managed by WMI (internal monitor)
	/// </summary>
	internal class WmiMonitorItem : MonitorItem
	{
		private readonly bool _isInternal;
		private readonly byte[] _brightnessLevels;

		public WmiMonitorItem(
			string deviceInstanceId,
			string description,
			byte displayIndex,
			byte monitorIndex,
			Rect monitorRect,
			bool isInternal,
			IEnumerable<byte> brightnessLevels) : base(
				deviceInstanceId: deviceInstanceId,
				description: description,
				displayIndex: displayIndex,
				monitorIndex: monitorIndex,
				monitorRect: monitorRect,
				isReachable: true)
		{
			this._isInternal = isInternal;
			this._brightnessLevels = brightnessLevels?.ToArray() ?? throw new ArgumentNullException(nameof(brightnessLevels));
		}

		public override AccessResult UpdateBrightness(int brightness = -1)
		{
			if (_isInternal)
			{
				this.Brightness = PowerManagement.GetActiveSchemeBrightness();

				this.BrightnessSystemAdjusted = !PowerManagement.IsAdaptiveBrightnessEnabled
					? -1 // Default
					: (0 <= brightness)
						? brightness
						: MSMonitor.GetBrightness(DeviceInstanceId);
			}
			else
			{
				this.Brightness = (0 <= brightness)
					? brightness
					: MSMonitor.GetBrightness(DeviceInstanceId);
			}
			return (0 <= this.Brightness) ? AccessResult.Succeeded : AccessResult.Failed;
		}

		public override AccessResult SetBrightness(int brightness)
		{
			if (brightness is < 0 or > 100)
				throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be within 0 to 100.");

			if (_isInternal)
			{
				if (PowerManagement.SetActiveSchemeBrightness(brightness))
				{
					this.Brightness = brightness;
					return AccessResult.Succeeded;
				}
			}
			else
			{
				brightness = ArraySearch.GetNearest(_brightnessLevels, (byte)brightness);

				if (MSMonitor.SetBrightness(DeviceInstanceId, brightness))
				{
					this.Brightness = brightness;
					return AccessResult.Succeeded;
				}
			}
			return AccessResult.Failed;
		}
	}
}