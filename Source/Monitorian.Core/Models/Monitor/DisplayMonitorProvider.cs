using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Monitorian.Core.Models.Monitor;

internal interface IDisplayItem
{
	public string DeviceInstanceId { get; }
	public string DisplayName { get; }
	public bool IsInternal { get; }
	public string ConnectionDescription { get; }
}

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
	public record DisplayItem : IDisplayItem
	{
		/// <summary>
		/// Device ID (Not device interface ID)
		/// </summary>
		[DataMember(Order = 0)]
		public string DeviceInstanceId { get; }

		/// <summary>
		/// Display name
		/// </summary>
		[DataMember(Order = 1)]
		public string DisplayName { get; }

		/// <summary>
		/// Native resolution in raw pixels.
		/// </summary>
		[DataMember(Order = 2)]
		public Size NativeResolution { get; }

		/// <summary>
		/// Physical size in inches
		/// </summary>
		[DataMember(Order = 3)]
		public Size PhysicalSize { get; }

		/// <summary>
		/// Physical diagonal Length in inches
		/// </summary>
		[DataMember(Order = 4)]
		public float PhysicalDiagonalLength { get; }

		/// <summary>
		/// Whether the display is connected internally
		/// </summary>
		[DataMember(Order = 5)]
		public bool IsInternal { get; }

		/// <summary>
		/// Connection description
		/// </summary>
		[DataMember(Order = 6)]
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
			this.PhysicalDiagonalLength = GetDiagonal(PhysicalSize);
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
			var items = new List<DisplayItem>();
			var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Display.DisplayMonitor.GetDeviceSelector(), [deviceInstanceIdKey]);
			if (devices is { Count: > 0 })
			{
				foreach (var device in devices)
				{
					if (!device.Properties.TryGetValue(deviceInstanceIdKey, out object value))
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
					//Debug.WriteLine($"NativeResolution: {displayMonitor.NativeResolutionInRawPixels.Width},{displayMonitor.NativeResolutionInRawPixels.Height}");
					//Debug.WriteLine($"PhysicalSize: {displayMonitor.PhysicalSizeInInches.Value.Width:F2},{displayMonitor.PhysicalSizeInInches.Value.Height:F2}");
					//Debug.WriteLine($"ConnectionKind: {displayMonitor.ConnectionKind}");

					items.Add(new DisplayItem(
						deviceInstanceId: deviceInstanceId,
						displayName: displayMonitor.DisplayName,
						nativeResolution: displayMonitor.NativeResolutionInRawPixels,
						physicalSize: displayMonitor.PhysicalSizeInInches ?? default,
						isInternal: (displayMonitor.ConnectionKind == Windows.Devices.Display.DisplayMonitorConnectionKind.Internal),
						connectionDescription: GetConnectionDescription(displayMonitor.ConnectionKind, displayMonitor.PhysicalConnector)));
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

	private static string GetConnectionDescription(Windows.Devices.Display.DisplayMonitorConnectionKind connectionKind, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind connectorKind)
	{
		switch (connectionKind)
		{
			case Windows.Devices.Display.DisplayMonitorConnectionKind.Internal:
			case Windows.Devices.Display.DisplayMonitorConnectionKind.Virtual:
			case Windows.Devices.Display.DisplayMonitorConnectionKind.Wireless:
				return connectionKind.ToString();

			case Windows.Devices.Display.DisplayMonitorConnectionKind.Wired:
				switch (connectorKind)
				{
					case Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.HD15:
						return "VGA";

					case Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.AnalogTV:
					case Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.DisplayPort:
						return connectorKind.ToString();

					case Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Dvi:
					case Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Hdmi:
					case Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Lvds:
					case Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Sdi:
						return connectorKind.ToString().ToUpper();
				}
				break;
		}
		return null;
	}
}