using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Monitorian.Core.Helper;

namespace Monitorian.Core.Models.Sensor;

public static class LightSensor
{
	/// <summary>
	/// A wrapper class of <see cref="Windows.Devices.Sensors.LightSensor"/>
	/// </summary>
	/// <remarks>
	/// <see cref="Windows.Devices.Sensors.LightSensor"/> has been available since Windows 8.1
	/// but is officially supported on Windows 10 (version 10.0.10240.0) or greater.
	/// </remarks>
	public class WinRTLightSensor
	{
		public static bool AmbientLightSensorExists()
		{
			// Referring Windows.Devices.Sensors.LightSensor itself makes this method to
			// cause System.TypeLoadException when it is called on Windows 7 regardless of
			// the procedure in this method.

			return (Windows.Devices.Sensors.LightSensor.GetDefault() is not null);
		}

		/// <summary>
		/// Attempts to get ambient light illuminance.
		/// </summary>
		/// <param name="illuminance">Illuminance in lux</param>
		/// <returns>True if successfully gets</returns>
		public static bool TryGetAmbientLight(out float illuminance)
		{
			var reading = Windows.Devices.Sensors.LightSensor.GetDefault()?.GetCurrentReading();
			if (reading is null)
			{
				illuminance = default;
				return false;
			}
			illuminance = reading.IlluminanceInLux;
			return true;
		}

		private static Windows.Devices.Sensors.LightSensor _sensor;
		private static readonly object _lock = new();

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
						var sensor = _sensor ?? Windows.Devices.Sensors.LightSensor.GetDefault();
						if (sensor is not null)
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
		/// Occurs when ambient light illuminance has changed.
		/// </summary>
		/// <remarks>EventArgs indicates illuminance in lux.</remarks>
		public static event EventHandler<float> AmbientLightChanged
		{
			add
			{
				lock (_lock)
				{
					_sensor ??= Windows.Devices.Sensors.LightSensor.GetDefault();
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
					if (_ambientLightChanged is not null)
						return;

					_sensor.ReportInterval = 0; // Resetting to the default interval is necessary when ending subscription.
					_sensor.ReadingChanged -= OnReadingChanged;
					_sensor = null;
				}
			}
		}
		private static event EventHandler<float> _ambientLightChanged;

		private static void OnReadingChanged(Windows.Devices.Sensors.LightSensor sender, Windows.Devices.Sensors.LightSensorReadingChangedEventArgs args)
		{
			_ambientLightChanged?.Invoke(sender, args.Reading.IlluminanceInLux);
		}
	}

	public class ComLightSensor
	{
		#region COM

		// From SensorsApi.h
		[ComImport, Guid("BD77DB67-45A8-42DC-8D00-6DCF15F8377A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface ISensorManager
		{
			[PreserveSig]
			uint GetSensorsByCategory(
				[MarshalAs(UnmanagedType.LPStruct)] Guid sensorCategory,
				[MarshalAs(UnmanagedType.Interface)] out ISensorCollection ppSensorsFound);

			[PreserveSig]
			uint GetSensorsByType(
				[MarshalAs(UnmanagedType.LPStruct)] Guid sensorType,
				[MarshalAs(UnmanagedType.Interface)] out ISensorCollection ppSensorsFound);

			[PreserveSig]
			uint GetSensorByID(
				[MarshalAs(UnmanagedType.LPStruct)] Guid sensorID,
				[MarshalAs(UnmanagedType.Interface)] out ISensor ppSensor);
		}

		[ComImport, Guid("77A1C827-FCD2-4689-8915-9D613CC5FA3E"), ClassInterface(ClassInterfaceType.None)]
		private class SensorManager
		{ }

		[ComImport, Guid("23571E11-E545-4DD8-A337-B89BF44B10DF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface ISensorCollection
		{
			[PreserveSig]
			uint GetAt(
				uint ulIndex,
				[MarshalAs(UnmanagedType.Interface)] out ISensor ppSensor);

			[PreserveSig]
			uint GetCount(out uint pCount);
		}

		[ComImport, Guid("5FA08F80-2657-458E-AF75-46F73FA6AC5C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface ISensor
		{
			[PreserveSig]
			uint GetID([MarshalAs(UnmanagedType.LPStruct)] out Guid id);

			[PreserveSig]
			uint GetCategory([MarshalAs(UnmanagedType.LPStruct)] out Guid sensorCategory);

			[PreserveSig]
			uint GetType([MarshalAs(UnmanagedType.LPStruct)] out Guid sensorType);

			[PreserveSig]
			uint GetFriendlyName(out string friendlyName);
		}

		private const uint S_OK = 0x0;
		private const uint E_ELEMENTNOTFOUND = 0x80070490; // 0x80070490 means 0x0490 -> 1168 -> ERROR_NOT_FOUND

		#endregion

		public static bool AmbientLightSensorExists() => SensorExists(SENSOR_TYPE_AMBIENT_LIGHT);

		private static Guid SENSOR_TYPE_AMBIENT_LIGHT => new("97F115C8-599A-4153-8894-D2D12899918A");

		private static bool SensorExists(Guid sensorTypeGuid)
		{
			ISensorManager sensorManager = null;
			ISensorCollection sensorCollection = null;
			try
			{
				sensorManager = (ISensorManager)new SensorManager();

				var result = sensorManager.GetSensorsByType(
					sensorTypeGuid,
					out sensorCollection);
				if (result != S_OK)
				{
					if (result == E_ELEMENTNOTFOUND)
					{
						Debug.WriteLine("The sensor of a specified type is not found.");
					}
					return false;
				}

				sensorCollection.GetCount(out uint count);
				return (0 < count);
			}
			catch (COMException ex)
			{
				switch ((uint)ex.HResult)
				{
					case 0x800704EC:
						// Message: This program is blocked by group policy. For more information, 
						// contact your system administrator. (Exception from HRESULT: 0x800704EC).
						// 0x800704EC means 0x04EC -> 1260 -> ERROR_ACCESS_DISABLED_BY_POLICY
						return false;

					case 0x80040154:
						// Message: Class not registered (Exception from HRESULT: 0x80040154 
						// (REGDB_E_CLASSNOTREG)).
						return false;
				}
				throw;
			}
			finally
			{
				if (sensorManager is not null)
					Marshal.FinalReleaseComObject(sensorManager);

				if (sensorCollection is not null)
					Marshal.FinalReleaseComObject(sensorCollection);
			}
		}
	}

	/// <summary>
	/// Determines whether an integrated ambient light sensor exists.
	/// </summary>
	/// <returns>True if exists</returns>
	public static bool AmbientLightSensorExists => _ambientLightSensorExists.Value;
	private static readonly Lazy<bool> _ambientLightSensorExists = new(() =>
	{
		return OsVersion.Is10OrGreater
			? WinRTLightSensor.AmbientLightSensorExists()
			: ComLightSensor.AmbientLightSensorExists();
	});
}