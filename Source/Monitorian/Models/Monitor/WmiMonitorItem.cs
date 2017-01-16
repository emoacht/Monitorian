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
			if (brightnessLevels == null)
				throw new ArgumentNullException(nameof(brightnessLevels));

			this._brightnessLevels = brightnessLevels;
			this._isRemovable = isRemovable;
		}

		public bool UpdateBrightness(int brightness = -1)
		{
			if (_isRemovable)
			{
				if (0 <= brightness)
				{
					this.Brightness = brightness;
				}
				else
				{
					this.Brightness = MSMonitor.GetBrightness(DeviceInstanceId);
				}
			}
			else
			{
				this.Brightness = PowerManagement.GetActiveSchemeBrightness();
			}
			return (0 <= this.Brightness);
		}

		public bool SetBrightness(int brightness)
		{
			if ((brightness < 0) || (100 < brightness))
				throw new ArgumentOutOfRangeException(nameof(brightness));

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