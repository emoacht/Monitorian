using System;
using System.Linq;

namespace Monitorian.Core.Models.Monitor;

public static class DeviceConversion
{
	/// <summary>
	/// Converts device path to device instance ID.
	/// </summary>
	/// <param name="devicePath">Device path</param>
	/// <returns>Device instance ID</returns>
	internal static string ConvertToDeviceInstanceId(string devicePath)
	{
		// The typical format of device path is as follows:
		// \\?\DISPLAY#<hardware-specific-ID>#<instance-specific-ID>#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}
		// \\?\ is extended-length path prefix.
		// DISPLAY indicates display device.
		// {e6f07b5f-ee97-4a90-b076-33f57bf4eaa7} means GUID_DEVINTERFACE_MONITOR.

		int index = devicePath.IndexOf("DISPLAY", StringComparison.Ordinal);
		if (index < 0)
			return null;

		var fields = devicePath.Substring(index).Split('#');
		if (fields.Length < 3)
			return null;

		return string.Join(@"\", fields.Take(3));
	}

	/// <summary>
	/// Attempts to parse a specified string to device instance ID.
	/// </summary>
	/// <param name="source">Source string</param>
	/// <param name="deviceInstanceId">Device instance ID</param>
	/// <returns>True if successfully parses</returns>
	/// <remarks>This method can accept escaped backslashes in JSON.</remarks>
	public static bool TryParseToDeviceInstanceId(string source, out string deviceInstanceId)
	{
		// The typical format of device instance ID is as follows:
		// DISPLAY\<hardware-specific-ID>\<instance-specific-ID>
		// hardware-specific-ID usually includes manufacturer-specific characters.

		var buffer = source?.Trim();
		if (!string.IsNullOrEmpty(buffer))
		{
			if (buffer.StartsWith("DISPLAY", StringComparison.Ordinal))
			{
				var fields = buffer.Split([@"\"], StringSplitOptions.RemoveEmptyEntries);
				if (fields is { Length: 3 })
				{
					deviceInstanceId = string.Join(@"\", fields);
					return true;
				}
			}
		}
		deviceInstanceId = null;
		return false;
	}
}