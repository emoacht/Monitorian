using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Monitor
{
	/// <summary>
	/// Physical monitor managed by WMI (internal monitor)
	/// </summary>
	internal class WmiMonitorItem : MonitorItem
	{
		private readonly byte[] _brightnessLevels;
		private readonly bool _isRemovable;

		public WmiMonitorItem(
			string deviceInstanceId,
			string description,
			byte displayIndex,
			byte monitorIndex,
			IEnumerable<byte> brightnessLevels,
			bool isRemovable) : base(
				deviceInstanceId: deviceInstanceId,
				description: description,
				displayIndex: displayIndex,
				monitorIndex: monitorIndex,
				isReachable: true)
		{
			this._brightnessLevels = brightnessLevels?.ToArray() ?? throw new ArgumentNullException(nameof(brightnessLevels));
			this._isRemovable = isRemovable;
		}

		public override AccessResult UpdateBrightness(int brightness = -1)
		{
			if (_isRemovable)
			{
				this.Brightness = (0 <= brightness)
					? brightness
					: MSMonitor.GetBrightness(DeviceInstanceId);
			}
			else
			{
				this.Brightness = PowerManagement.GetActiveSchemeBrightness();

				this.BrightnessSystemAdjusted = !PowerManagement.IsAdaptiveBrightnessEnabled
					? -1 // Default
					: (0 <= brightness)
						? brightness
						: MSMonitor.GetBrightness(DeviceInstanceId);
			}
			return (0 <= this.Brightness) ? AccessResult.Succeeded : AccessResult.Failed;
		}

		public override AccessResult SetBrightness(int brightness)
		{
			if (brightness is < 0 or > 100)
				throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be within 0 to 100.");

			if (_isRemovable)
			{
				brightness = ArraySearch.GetNearest(_brightnessLevels, (byte)brightness);

				if (MSMonitor.SetBrightness(DeviceInstanceId, brightness))
				{
					this.Brightness = brightness;
					return AccessResult.Succeeded;
				}
			}
			else
			{
				if (PowerManagement.SetActiveSchemeBrightness(brightness))
				{
					this.Brightness = brightness;
					return AccessResult.Succeeded;
				}
			}
			return AccessResult.Failed;
		}
	}
}