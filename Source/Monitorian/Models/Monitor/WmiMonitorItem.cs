using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Helper;

namespace Monitorian.Models.Monitor
{
	/// <summary>
	/// Physical monitor managed by WMI (internal monitor)
	/// </summary>
	internal class WmiMonitorItem : MonitorItem, IMonitor
	{
		private readonly byte[] _brightnessLevels;
		private readonly bool _isRemovable;

		public WmiMonitorItem(
			string description,
			string deviceInstanceId,
			byte displayIndex,
			byte monitorIndex,
			byte[] brightnessLevels,
			bool isRemovable) : base(
				description,
				deviceInstanceId,
				displayIndex,
				monitorIndex)
		{
			this._brightnessLevels = brightnessLevels ?? throw new ArgumentNullException(nameof(brightnessLevels));
			this._isRemovable = isRemovable;
		}

		private readonly object _lock = new object();

		public bool UpdateBrightness(int brightness = -1)
		{
			lock (_lock)
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

					if (LightSensor.AmbientLightSensorExists)
					{
						this.BrightnessAdjusted = (0 <= brightness)
							? brightness
							: MSMonitor.GetBrightness(DeviceInstanceId);
					}
				}
				return (0 <= this.Brightness);
			}
		}

		public bool SetBrightness(int brightness)
		{
			if ((brightness < 0) || (100 < brightness))
				throw new ArgumentOutOfRangeException(nameof(brightness), "The brightness must be within 0 to 100.");

			lock (_lock)
			{
				if (_isRemovable)
				{
					brightness = ArraySearch.GetNearest(_brightnessLevels, (byte)brightness);

					if (MSMonitor.SetBrightness(DeviceInstanceId, brightness))
					{
						this.Brightness = brightness;
						return true;
					}
				}
				else
				{
					if (PowerManagement.SetActiveSchemeBrightness(brightness))
					{
						this.Brightness = brightness;
						return true;
					}
				}
				return false;
			}
		}
	}
}