using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;

namespace Monitorian.Supplement
{
	/// <summary>
	/// A wrapper class of <see cref="Windows.Devices.Display.DisplayMonitor"/>
	/// </summary>
	/// <remarks>
	/// <see cref="Windows.Devices.Display.DisplayMonitor"/> is only available
	/// on Windows 10 (version 10.0.17134.0) or newer.
	/// </remarks>
	public static class DisplayInformation
	{
		#region Type

		/// <summary>
		/// Display monitor information
		/// </summary>
		public record DisplayItem
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
			/// Native resolution in raw pixels.
			/// </summary>
			public Size NativeResolution { get; }

			/// <summary>
			/// Physical size in inches
			/// </summary>
			public Size PhysicalSize { get; }

			/// <summary>
			/// Physical diagonal Length in inches
			/// </summary>
			public float PhysicalDiagonalLength => GetDiagonal(PhysicalSize);

			/// <summary>
			/// Whether the display is connected internally
			/// </summary>
			public bool IsInternal { get; }

			/// <summary>
			/// Connection description
			/// </summary>
			public string ConnectionDescription { get; }

			internal DisplayItem(
				string deviceInstanceId,
				string displayName,
				Windows.Graphics.SizeInt32 nativeResolution,
				Windows.Foundation.Size physicalSize,
				bool isInternal,
				string connectionDescription)
			{
				this.DeviceInstanceId = deviceInstanceId;
				this.DisplayName = displayName;
				this.NativeResolution = new Size(nativeResolution.Width, nativeResolution.Height);
				this.PhysicalSize = new Size(physicalSize.Width, physicalSize.Height);
				this.IsInternal = isInternal;
				this.ConnectionDescription = connectionDescription;
			}

			private static float GetDiagonal(Size source) =>
				(float)Math.Sqrt(Math.Pow(source.Width, 2) + Math.Pow(source.Height, 2));
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
				if (devices is { Count: > 0 })
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
						//Debug.WriteLine($"NativeResolution: {displayMonitor.NativeResolutionInRawPixels.Width},{displayMonitor.NativeResolutionInRawPixels.Height}");
						//Debug.WriteLine($"PhysicalSize: {displayMonitor.PhysicalSizeInInches.Value.Width:F2},{displayMonitor.PhysicalSizeInInches.Value.Height:F2}");
						//Debug.WriteLine($"ConnectionKind: {displayMonitor.ConnectionKind}");

						items.Add(new DisplayItem(
							deviceInstanceId: deviceInstanceId,
							displayName: displayMonitor.DisplayName,
							nativeResolution: displayMonitor.NativeResolutionInRawPixels,
							physicalSize: displayMonitor.PhysicalSizeInInches ?? default,
							isInternal: (displayMonitor.ConnectionKind == DisplayMonitorConnectionKind.Internal),
							connectionDescription: GetConnectionDescription(displayMonitor.ConnectionKind, displayMonitor.PhysicalConnector)));
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
	}
}