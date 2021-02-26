using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Models.Monitor
{
	internal class LightSensor
	{
		#region COM

		/// <summary>
		/// A partial wrapper for ISensorManager interface
		/// </summary>
		/// <remarks>This interface is defined in SensorsApi.h.</remarks>
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

		/// <summary>
		/// A wrapper for SensorManager class
		/// </summary>
		/// <remarks>This class is defined in SensorsApi.h.</remarks>
		[ComImport, Guid("77A1C827-FCD2-4689-8915-9D613CC5FA3E"), ClassInterface(ClassInterfaceType.None)]
		private class SensorManager
		{ }

		/// <summary>
		/// A partial wrapper for ISensorCollection interface
		/// </summary>
		/// <remarks>This interface is defined in SensorsApi.h.</remarks>
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

		/// <summary>
		/// A partial wrapper for ISensor interface
		/// </summary>
		/// <remarks>This interface is defined in SensorsApi.h.</remarks>
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

		private static uint S_OK = 0x0;
		private static uint E_ELEMENTNOTFOUND = 0x80070490; // 0x80070490 means 0x0490 -> 1168 -> ERROR_NOT_FOUND

		#endregion

		private static Guid SENSOR_TYPE_AMBIENT_LIGHT => new Guid("97F115C8-599A-4153-8894-D2D12899918A");

		public static bool AmbientLightSensorExists => _ambientLightSensorExists.Value;
		private static readonly Lazy<bool> _ambientLightSensorExists = new Lazy<bool>(() => SensorExists(SENSOR_TYPE_AMBIENT_LIGHT));

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
}