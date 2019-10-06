using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;

namespace Monitorian.Supplement
{
	/// <summary>
	/// A wrapper class of <see cref="Windows.Devices.Sensors.LightSensor"/>
	/// </summary>
	/// <remarks>
	/// <see cref="Windows.Devices.Sensors.LightSensor"/> has been available
	/// since Windows 8.1 but is officially supported on Windows 10 (version 10.0.10240.0) or newer.
	/// </remarks>
	public class LightInformation
	{
		/// <summary>
		/// Whether an integrated ambient light sensor exists.
		/// </summary>
		/// <returns>True if exists</returns>
		public static bool AmbientLightSensorExists()
		{
			return (LightSensor.GetDefault() != null);
		}

		/// <summary>
		/// Attempts to get ambient light illuminance.
		/// </summary>
		/// <param name="illuminance">Illuminance in lux</param>
		/// <returns>True if successfully gets</returns>
		public static bool TryGetAmbientLight(out float illuminance)
		{
			var reading = LightSensor.GetDefault()?.GetCurrentReading();
			if (reading is null)
			{
				illuminance = default;
				return false;
			}
			illuminance = reading.IlluminanceInLux;
			return true;
		}

		private static LightSensor _sensor;
		private static readonly object _lock = new object();

		/// <summary>
		/// Report interval for ambient light sensor
		/// </summary>
		public static TimeSpan ReportInterval
		{
			get => TimeSpan.FromMilliseconds(_reportInterval);
			set
			{
				lock (_lock)
				{
					_reportInterval = 0;
					if (TimeSpan.Zero < value)
					{
						var sensor = _sensor ?? LightSensor.GetDefault();
						if (sensor != null)
						{
							_reportInterval = Math.Max(sensor.MinimumReportInterval, (uint)value.TotalMilliseconds);
						}
					}

					if (_sensor is null)
						return;

					_sensor.ReportInterval = _reportInterval; // Setting zero requests the sensor to use its default interval.
				}
			}
		}
		private static uint _reportInterval = 0;

		/// <summary>
		/// Ambient light illuminance changed event
		/// </summary>
		/// <remarks>EventArgs indicates illuminance in lux.</remarks>
		public static event EventHandler<float> AmbientLightChanged
		{
			add
			{
				lock (_lock)
				{
					_sensor ??= LightSensor.GetDefault();
					if (_sensor is null)
						return;

					_ambientLightChanged += value;
					if (_ambientLightChanged.GetInvocationList().Length > 1)
						return;

					_sensor.ReportInterval = _reportInterval;
					_sensor.ReadingChanged += OnReadingChanged;
				}
			}
			remove
			{
				lock (_lock)
				{
					if (_sensor is null)
						return;

					_ambientLightChanged -= value;
					if (_ambientLightChanged != null)
						return;

					_sensor.ReportInterval = 0; // Resetting to the default interval is necessary when ending subscription.
					_sensor.ReadingChanged -= OnReadingChanged;
					_sensor = null;
				}
			}
		}
		private static event EventHandler<float> _ambientLightChanged;

		private static void OnReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
		{
			_ambientLightChanged?.Invoke(sender, args.Reading.IlluminanceInLux);
		}
	}
}