using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Monitorian.Supplement
{
	/// <summary>
	/// A wrapper class of <see cref="Windows.Devices.Display.DisplayMonitor"/>
	/// </summary>
	/// <remarks>
	/// <see cref="Windows.Devices.Display.DisplayMonitor"/> is only available
	/// on Windows 10 (version 10.0.17134.0) or newer.
	/// </remarks>
	public class DisplayInformation
	{
		#region Type

		/// <summary>
		/// Display monitor information
		/// </summary>
		public class DisplayItem
		{
			/// <summary>
			/// Device ID (Not device interface ID)
			/// </summary>
			public string DeviceInstanceId { get; }

			/// <summary>
			/// Display name
			/// </summary>
			public string DisplayName { get; }

			/// <summary>
			/// Whether the display is connected internally
			/// </summary>
			public bool IsInternal { get; }

			/// <summary>
			/// Connection description
			/// </summary>
			public string ConnectionDescription { get; }

			/// <summary>
			/// Physical size (diagonal) in inches
			/// </summary>
			public float PhysicalSize { get; }

			internal DisplayItem(
				string deviceInstanceId,
				string displayName,
				bool isInternal,
				string connectionDescription = null,
				float physicalSize = 0F)
			{
				this.DeviceInstanceId = deviceInstanceId;
				this.DisplayName = displayName;
				this.IsInternal = isInternal;
				this.ConnectionDescription = connectionDescription;
				this.PhysicalSize = physicalSize;
			}
		}

		#endregion

		// System error code: 0x80070057 = 0x57 = ERROR_INVALID_PARAMETER
		// Error message: The parameter is incorrect.
		private const uint ERROR_INVALID_PARAMETER = 0x80070057;

		// System error code: 0x8007001F = 0x1F = ERROR_GEN_FAILURE
		// Error message: A device attached to the system is not functioning.
		private const uint ERROR_GEN_FAILURE = 0x8007001F;

		// System error code: 0x8002802B
		// Error message: Element not found.
		private const uint TYPE_E_ELEMENTNOTFOUND = 0x8002802B;

		/// <summary>
		/// Gets display monitor information.
		/// </summary>
		/// <returns>Array of display monitor information</returns>
		public static async Task<DisplayItem[]> GetDisplayMonitorsAsync()
		{
			const string deviceInstanceIdKey = "System.Devices.DeviceInstanceId";

			try
			{
				var devices = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector(), new[] { deviceInstanceIdKey });
				if (devices?.Any() is true)
				{
					var items = new List<DisplayItem>(devices.Count);
					foreach (var device in devices)
					{
						if (!device.Properties.TryGetValue(deviceInstanceIdKey, out object value))
							continue;

						var deviceInstanceId = value as string;
						if (string.IsNullOrWhiteSpace(deviceInstanceId))
							continue;

						var displayMonitor = await DisplayMonitor.FromInterfaceIdAsync(device.Id);
						//var displayMonitor = await DisplayMonitor.FromIdAsync(deviceInstanceId);
						if (displayMonitor is null)
							continue;

						//Debug.WriteLine($"DeviceInstanceId: {deviceInstanceId}");
						//Debug.WriteLine($"DisplayName: {displayMonitor.DisplayName}");
						//Debug.WriteLine($"ConnectionKind: {displayMonitor.ConnectionKind}");
						//Debug.WriteLine($"PhysicalConnector: {displayMonitor.PhysicalConnector}");
						//Debug.WriteLine($"PhysicalSize: {GetDiagonal(displayMonitor.PhysicalSizeInInches):F1}");

						items.Add(new DisplayItem(
							deviceInstanceId: deviceInstanceId,
							displayName: displayMonitor.DisplayName,
							isInternal: (displayMonitor.ConnectionKind == DisplayMonitorConnectionKind.Internal),
							connectionDescription: GetConnectionDescription(displayMonitor.ConnectionKind, displayMonitor.PhysicalConnector),
							physicalSize: GetDiagonal(displayMonitor.PhysicalSizeInInches)));
					}
					return items.ToArray();
				}
			}
			catch (ArgumentException ax) when ((uint)ax.HResult is ERROR_INVALID_PARAMETER)
			{
			}
			catch (Exception ex) when ((uint)ex.HResult is ERROR_GEN_FAILURE or TYPE_E_ELEMENTNOTFOUND)
			{
			}
			return Array.Empty<DisplayItem>();
		}

		private static string GetConnectionDescription(DisplayMonitorConnectionKind connectionKind, DisplayMonitorPhysicalConnectorKind connectorKind)
		{
			switch (connectionKind)
			{
				case DisplayMonitorConnectionKind.Internal:
				case DisplayMonitorConnectionKind.Virtual:
				case DisplayMonitorConnectionKind.Wireless:
					return connectionKind.ToString();

				case DisplayMonitorConnectionKind.Wired:
					switch (connectorKind)
					{
						case DisplayMonitorPhysicalConnectorKind.HD15:
							return "VGA";

						case DisplayMonitorPhysicalConnectorKind.AnalogTV:
						case DisplayMonitorPhysicalConnectorKind.DisplayPort:
							return connectorKind.ToString();

						case DisplayMonitorPhysicalConnectorKind.Dvi:
						case DisplayMonitorPhysicalConnectorKind.Hdmi:
						case DisplayMonitorPhysicalConnectorKind.Lvds:
						case DisplayMonitorPhysicalConnectorKind.Sdi:
							return connectorKind.ToString().ToUpper();
					}
					break;
			}
			return null;
		}

		private static float GetDiagonal(Size? source) => source.HasValue
			? (float)Math.Sqrt(Math.Pow(source.Value.Width, 2) + Math.Pow(source.Value.Height, 2))
			: 0F;
	}
}