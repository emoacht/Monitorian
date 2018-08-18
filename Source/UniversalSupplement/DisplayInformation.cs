using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;

namespace UniversalSupplement
{
	/// <summary>
	/// DisplayMonitor functions
	/// </summary>
	/// <remarks>
	/// This class wraps <see cref="Windows.Devices.Display.DisplayMonitor"/> class which is only available
	/// on Windows 10 (Redstone 4, 1803 = version 10.0.17134.0) or newer.
	/// </remarks>
	public class DisplayInformation
	{
		#region Type

		/// <summary>
		/// Display monitor information
		/// </summary>
		[DataContract]
		public class DisplayItem
		{
			/// <summary>
			/// Device ID (Not device interface ID)
			/// </summary>
			[DataMember(Order = 0)]
			public string DeviceInstanceId { get; private set; }

			/// <summary>
			/// Display name
			/// </summary>
			[DataMember(Order = 1)]
			public string DisplayName { get; private set; }

			/// <summary>
			/// Whether the display is connected internally.
			/// </summary>
			[DataMember(Order = 2)]
			public bool IsInternal { get; private set; }

			internal DisplayItem(
				string deviceInstanceId,
				string displayName,
				bool isInternal)
			{
				this.DeviceInstanceId = deviceInstanceId;
				this.DisplayName = displayName;
				this.IsInternal = isInternal;
			}
		}

		#endregion

		/// <summary>
		/// Gets display monitor information.
		/// </summary>
		/// <returns>Array of display monitor information</returns>
		public static async Task<DisplayItem[]> GetDisplayMonitorsAsync()
		{
			const string deviceInstanceIdKey = "System.Devices.DeviceInstanceId";

			var devices = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector(), new[] { deviceInstanceIdKey });

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

				//Debug.WriteLine($"DeviceInstanceId: {deviceInstanceId}");
				//Debug.WriteLine($"DeviceName: {device.Name}");
				//Debug.WriteLine($"DisplayName: {displayMonitor.DisplayName}");
				//Debug.WriteLine($"ConnectionKind: {displayMonitor.ConnectionKind}");

				items.Add(new DisplayItem(
					deviceInstanceId: deviceInstanceId,
					displayName: displayMonitor.DisplayName,
					isInternal: (displayMonitor.ConnectionKind == DisplayMonitorConnectionKind.Internal)));
			}
			return items.ToArray();
		}
	}
}
