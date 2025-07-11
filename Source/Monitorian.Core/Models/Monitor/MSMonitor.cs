﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.Serialization;
using System.Text;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// MSMonitorClass Functions
/// </summary>
internal class MSMonitor
{
	#region Type

	[DataContract]
	public class DesktopItem
	{
		[DataMember(Order = 0)]
		public string DeviceInstanceId { get; }

		[DataMember(Order = 1)]
		public string Description { get; }

		public byte[] BrightnessLevels { get; }
		[DataMember(Order = 2, Name = nameof(BrightnessLevels))]
		private string _brightnessLevelsString;

		[OnSerializing]
		private void OnSerializing(StreamingContext context)
		{
			_brightnessLevelsString = string.Join(" ", BrightnessLevels ?? []);
		}

		public DesktopItem(
			string deviceInstanceId,
			string description)
		{
			this.DeviceInstanceId = deviceInstanceId;
			this.Description = description;
		}

		public DesktopItem(
			string deviceInstanceId,
			string description,
			byte[] brightnessLevels) : this(
				deviceInstanceId: deviceInstanceId,
				description: description)
		{
			this.BrightnessLevels = brightnessLevels;
		}
	}

	#endregion

	public static IEnumerable<DesktopItem> EnumerateDesktopMonitors()
	{
		if (_isBrightnessEventWatchable is false)
			yield break;

		var monitors = new List<DesktopItem>();

		using (var @class = new ManagementClass("Win32_DesktopMonitor"))
		{
			try
			{
				using var instances = @class.GetInstances();

				foreach (ManagementObject instance in instances)
				{
					using (instance)
					{
						var pnpDeviceId = (string)instance.GetPropertyValue("PNPDeviceID");
						if (string.IsNullOrWhiteSpace(pnpDeviceId))
							continue;

						var description = (string)instance.GetPropertyValue("Description");
						if (string.IsNullOrWhiteSpace(description))
							continue;

						monitors.Add(new DesktopItem(
							deviceInstanceId: pnpDeviceId,
							description: description));
					}
				}
			}
			catch (ManagementException me)
			{
				Debug.WriteLine($"Failed to get and enumerate instances by Win32_DesktopMonitor. HResult: 0x{me.HResult:X8} ErrorCode: {me.ErrorCode}" + Environment.NewLine
					+ me);
				yield break;
			}
		}

		using (var @class = new ManagementClass(@"root\wmi", "WmiMonitorBrightness", null))
		{
			ManagementObjectCollection instances = null;

			try
			{
				instances = @class.GetInstances();
			}
			catch (ManagementException me)
			{
				Debug.WriteLine($"Failed to get instances by WmiMonitorBrightness. HResult: 0x{me.HResult:X8} ErrorCode: {me.ErrorCode}" + Environment.NewLine
							+ me);
				yield break;
			}

			using (instances)
			{
				using var enumerator = instances.GetEnumerator();

				while (true)
				{
					try
					{
						if (!enumerator.MoveNext())
							break;
					}
					catch (ManagementException me)
					{
						// ManagementObjectCollection.ManagementObjectEnumerator.MoveNext method for 
						// WmiMonitorBrightness instance may throw a ManagementException when called
						// immediately after resume.
						// The ManagementException is caused by various reasons including:
						// ErrorCode is ManagementStatus.NotSupported,
						// ErrorCode is ManagementStatus.CallCanceled,
						// HResult is 0x80131501.
						Debug.WriteLine($"Failed to enumerate instances by WmiMonitorBrightness. HResult: 0x{me.HResult:X8} ErrorCode: {me.ErrorCode}" + Environment.NewLine
							+ me);
						yield break;
					}

					using var instance = (ManagementObject)enumerator.Current;

					var instanceName = (string)instance.GetPropertyValue("InstanceName");
					var monitor = monitors.FirstOrDefault(x => instanceName.StartsWith(x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
					if (monitor is null)
						continue;

					var level = (byte[])instance.GetPropertyValue("Level");

					//Debug.WriteLine($"DeviceInstanceId: {monitor.DeviceInstanceId}");
					//Debug.WriteLine($"Description: {monitor.Description}");
					//Debug.WriteLine($"Level length: {level.Length}");
					//Debug.WriteLine($"Active (unreliable): {(bool)instance["Active"]}");

					yield return new DesktopItem(
						deviceInstanceId: monitor.DeviceInstanceId,
						description: monitor.Description,
						brightnessLevels: level);
				}
			}
		}
	}

	public static int GetBrightness(string deviceInstanceId)
	{
		if (string.IsNullOrWhiteSpace(deviceInstanceId))
			throw new ArgumentNullException(nameof(deviceInstanceId));

		try
		{
			using var searcher = GetSearcher("WmiMonitorBrightness");
			using var instances = searcher.Get();

			foreach (ManagementObject instance in instances)
			{
				using (instance)
				{
					var instanceName = (string)instance.GetPropertyValue("InstanceName");
					if (instanceName.StartsWith(deviceInstanceId, StringComparison.OrdinalIgnoreCase))
						return (byte)instance.GetPropertyValue("CurrentBrightness");
				}
			}
			return -1;
		}
		catch (ManagementException me)
		{
			Debug.WriteLine($"Failed to get brightness by WmiMonitorBrightness. HResult: 0x{me.HResult:X8} ErrorCode: {me.ErrorCode}" + Environment.NewLine
				+ me);
			return -1;
		}
	}

	public static bool SetBrightness(string deviceInstanceId, int brightness, int timeout = int.MaxValue)
	{
		if (string.IsNullOrWhiteSpace(deviceInstanceId))
			throw new ArgumentNullException(nameof(deviceInstanceId));
		if (brightness is < 0 or > 100)
			throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be from 0 to 100.");

		try
		{
			using var searcher = GetSearcher("WmiMonitorBrightnessMethods");
			using var instances = searcher.Get();

			foreach (ManagementObject instance in instances)
			{
				using (instance)
				{
					var instanceName = (string)instance.GetPropertyValue("InstanceName");
					if (instanceName.StartsWith(deviceInstanceId, StringComparison.OrdinalIgnoreCase))
					{
						object result = instance.InvokeMethod("WmiSetBrightness", [(uint)timeout, (byte)brightness]);

						var isSuccess = (result is null); // Return value will be null if succeeded.
						if (!isSuccess)
						{
							var errorCode = (uint)result;
							isSuccess = (errorCode is 0);
							if (!isSuccess)
							{
								Debug.WriteLine($"Failed to set brightness. 0x{errorCode:X8}");
							}
						}
						return isSuccess;
					}
				}
			}
			return false;
		}
		catch (ManagementException me)
		{
			Debug.WriteLine($"Failed to set brightness by WmiSetBrightness. HResult: 0x{me.HResult:X8} ErrorCode: {me.ErrorCode}" + Environment.NewLine
				+ me);
			return false;
		}
	}

	public static (string productCode, string serialNumber) GetIds(string deviceInstanceId)
	{
		if (string.IsNullOrWhiteSpace(deviceInstanceId))
			throw new ArgumentNullException(nameof(deviceInstanceId));

		try
		{
			using var searcher = GetSearcher("WmiMonitorID");
			using var instances = searcher.Get();

			foreach (ManagementObject instance in instances)
			{
				using (instance)
				{
					var instanceName = (string)instance.GetPropertyValue("InstanceName");
					if (instanceName.StartsWith(deviceInstanceId, StringComparison.OrdinalIgnoreCase))
					{
						return (productCode: GetValue("ProductCodeID"),
								serialNumber: GetValue("SerialNumberID"));
					}
				}

				string GetValue(string propertyName)
				{
					var propertyValue = (ushort[])instance.GetPropertyValue(propertyName);
					if (propertyValue is not { Length: > 0 })
						return null;

					var buffer = propertyValue.Select(x => Convert.ToByte(x)).ToArray();
					return Encoding.ASCII.GetString(buffer).TrimEnd('\0');
				}
			}
			return default;
		}
		catch (ManagementException me)
		{
			Debug.WriteLine($"Failed to get IDs by WmiMonitorID. HResult: 0x{me.HResult:X8} ErrorCode: {me.ErrorCode}" + Environment.NewLine
				+ me);
			return default;
		}
	}

	private static ManagementObjectSearcher GetSearcher(string className) =>
		new(new ManagementScope(@"root\wmi"), new SelectQuery(className));

	#region Event Watcher

	private static bool? _isBrightnessEventWatchable = null;

	public static (ManagementEventWatcher watcher, string message) StartBrightnessEventWatcher()
	{
		try
		{
			var option = new EventWatcherOptions(null, TimeSpan.FromSeconds(1), 1);
			var watcher = new ManagementEventWatcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessEvent", option);

			watcher.Start();

			_isBrightnessEventWatchable = true;
			return (watcher, null);
		}
		catch (ManagementException me)
		{
			var message = $"Failed to start watcher for WmiMonitorBrightnessEvent. HResult: 0x{me.HResult:X8} ErrorCode: {me.ErrorCode}";
			Debug.WriteLine(message + Environment.NewLine
				+ me);

			_isBrightnessEventWatchable = false;
			return (null, message);
		}
	}

	public static (string instanceName, byte brightness) ParseBrightnessEventArgs(EventArrivedEventArgs e)
	{
		var newEvent = e.NewEvent;
		return ((string)newEvent["InstanceName"], (byte)newEvent["Brightness"]);
	}

	#endregion
}