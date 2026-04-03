using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// A factory class for <see cref="Windows.Devices.Display.DisplayMonitor"/>
/// </summary>
/// <remarks>
/// <see cref="Windows.Devices.Display.DisplayMonitor"/> is only available
/// on Windows 10 (version 10.0.17134.0) or greater.
/// </remarks>
internal class DisplayMonitorProvider
{
	#region Type

	[DataContract]
	public class DisplayItem
	{
		/// <summary>
		/// Device instance ID
		/// </summary>
		[DataMember(Order = 0)]
		public string DeviceInstanceId { get; }

		/// <summary>
		/// Display name
		/// </summary>
		[DataMember(Order = 1)]
		public string DisplayName { get; }

		/// <summary>
		/// Connection type
		/// </summary>
		[DataMember(Order = 2)]
		public ConnectionType Connection { get; }

		/// <summary>
		/// Whether the display is connected internally
		/// </summary>
		[DataMember(Order = 3)]
		public bool IsInternal { get; }

		/// <summary>
		/// Native resolution in raw pixels.
		/// </summary>
		[DataMember(Order = 4)]
		public Size NativeResolution { get; }

		/// <summary>
		/// Physical size in inches
		/// </summary>
		[DataMember(Order = 5)]
		public Size PhysicalSize { get; }

		/// <summary>
		/// Physical diagonal Length in inches
		/// </summary>
		[DataMember(Order = 6)]
		public float PhysicalDiagonalLength { get; }

		internal DisplayItem(
			string deviceInstanceId,
			string displayName,
			ConnectionType connection,
			bool isInternal,
			Windows.Graphics.SizeInt32 nativeResolution,
			Windows.Foundation.Size physicalSize)
		{
			this.DeviceInstanceId = deviceInstanceId;
			this.DisplayName = displayName;
			this.Connection = connection;
			this.IsInternal = isInternal;
			this.NativeResolution = new Size(nativeResolution.Width, nativeResolution.Height);
			this.PhysicalSize = new Size(physicalSize.Width, physicalSize.Height);
			this.PhysicalDiagonalLength = GetDiagonal(PhysicalSize);
		}

		private static float GetDiagonal(Size source) =>
			(float)Math.Sqrt(Math.Pow(source.Width, 2) + Math.Pow(source.Height, 2));
	}

	#endregion

	// Error message: The parameter is incorrect.
	// Error code: 0x80070057 -> 0x57 = ERROR_INVALID_PARAMETER
	private const uint ERROR_INVALID_PARAMETER = 0x80070057;

	// Error message: A device attached to the system is not functioning.
	// Error code: 0x8007001F -> 0x1F = ERROR_GEN_FAILURE
	private const uint ERROR_GEN_FAILURE = 0x8007001F;

	// Error message: Element not found.
	// Error code: 0x8002802B
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
			var items = new List<DisplayItem>();
			var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Display.DisplayMonitor.GetDeviceSelector(), [deviceInstanceIdKey]);
			if (devices is { Count: > 0 })
			{
				foreach (var device in devices)
				{
					// Null check is inserted because NullReferenceException is observed in this method.
					if ((device is null) || !device.Properties.TryGetValue(deviceInstanceIdKey, out object value))
						continue;

					var deviceInstanceId = value as string;
					if (string.IsNullOrWhiteSpace(deviceInstanceId))
						continue;

					var displayMonitor = await Windows.Devices.Display.DisplayMonitor.FromInterfaceIdAsync(device.Id);
					//var displayMonitor = await Windows.Devices.Display.DisplayMonitor.FromIdAsync(deviceInstanceId);
					if (displayMonitor is null)
						continue;

					//Debug.WriteLine($"DeviceInstanceId: {deviceInstanceId}");
					//Debug.WriteLine($"DisplayName: {displayMonitor.DisplayName}");
					//Debug.WriteLine($"ConnectionKind: {displayMonitor.ConnectionKind} {displayMonitor.PhysicalConnector}");
					//Debug.WriteLine($"NativeResolution: {displayMonitor.NativeResolutionInRawPixels.Width},{displayMonitor.NativeResolutionInRawPixels.Height}");
					//Debug.WriteLine($"PhysicalSize: {displayMonitor.PhysicalSizeInInches.Value.Width:F2},{displayMonitor.PhysicalSizeInInches.Value.Height:F2}");
					//Debug.WriteLine($"MinLuminanceInNits: {displayMonitor.MinLuminanceInNits}, MaxLuminanceInNits: {displayMonitor.MaxLuminanceInNits}");

					items.Add(new DisplayItem(
						deviceInstanceId: deviceInstanceId,
						displayName: displayMonitor.DisplayName,
						connection: GetConnectionType(displayMonitor.ConnectionKind, displayMonitor.PhysicalConnector),
						isInternal: (displayMonitor.ConnectionKind is Windows.Devices.Display.DisplayMonitorConnectionKind.Internal),
						nativeResolution: displayMonitor.NativeResolutionInRawPixels,
						physicalSize: displayMonitor.PhysicalSizeInInches ?? default));
				}
			}
#if DEBUG
			using var manager = Windows.Devices.Display.Core.DisplayManager.Create(Windows.Devices.Display.Core.DisplayManagerOptions.None);
			var state = manager.TryReadCurrentStateForAllTargets().State;
			foreach (var (path, target) in state.Views
				.SelectMany(x => x.Paths)
				.Select(x => (x, x.Target))
				.Where(x => x.Target.IsConnected))
			{
				var displayMonitor = target.TryGetMonitor();
				var deviceInstanceId = DeviceConversion.ConvertToDeviceInstanceId(displayMonitor.DeviceId);
				Debug.Assert(items.Any(x => x.DeviceInstanceId == deviceInstanceId));

				//var rate = path.PresentationRate.Value.VerticalSyncRate;
				//Debug.WriteLine($"RefreshRate: {rate.Numerator / (float)rate.Denominator}");
			}
#endif
			return items.ToArray();
		}
		catch (ArgumentException ax) when ((uint)ax.HResult is ERROR_INVALID_PARAMETER)
		{
		}
		catch (Exception ex) when ((uint)ex.HResult is ERROR_GEN_FAILURE or TYPE_E_ELEMENTNOTFOUND)
		{
		}
		return Array.Empty<DisplayItem>();
	}

	private static ConnectionType GetConnectionType(Windows.Devices.Display.DisplayMonitorConnectionKind connectionKind, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind connectorKind)
	{
		return (connectionKind, connectorKind) switch
		{
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Internal, _) => ConnectionType.Internal,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Virtual, _) => ConnectionType.Virtual,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wireless, _) => ConnectionType.Wireless,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wired, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.HD15) => ConnectionType.VGA,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wired, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.AnalogTV) => ConnectionType.AnalogTV,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wired, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Dvi) => ConnectionType.DVI,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wired, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Hdmi) => ConnectionType.HDMI,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wired, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Lvds) => ConnectionType.LVDS,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wired, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Sdi) => ConnectionType.SDI,
			(Windows.Devices.Display.DisplayMonitorConnectionKind.Wired, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.DisplayPort) => ConnectionType.DisplayPort,
			_ => ConnectionType.Unknown
		};
	}
}