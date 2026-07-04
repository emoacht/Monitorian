
using static Monitorian.Core.Models.Monitor.DisplayConfig;

namespace Monitorian.Core.Models.Monitor;

public enum ConnectionType
{
	Unknown = 0,
	Internal,

	Wired,
	VGA,
	AnalogTV,
	DVI,
	HDMI,
	LVDS,
	SDI,
	DisplayPort,
	SDTV,

	Wireless,
	Miracast,

	Virtual
}

internal static class ConnectionTypeConverter
{
	public static ConnectionType Convert(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology)
	{
		return outputTechnology switch
		{
			//DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 => ConnectionType.VGA,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_D_JPN => ConnectionType.AnalogTV,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI => ConnectionType.DVI,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI => ConnectionType.HDMI,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS => ConnectionType.LVDS,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDI => ConnectionType.SDI,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_USB_TUNNEL => ConnectionType.DisplayPort,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL or
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED => ConnectionType.Unknown,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDTVDONGLE => ConnectionType.SDTV,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST => ConnectionType.Miracast,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_WIRED => ConnectionType.Wired,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_VIRTUAL => ConnectionType.Wireless,
			DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL => ConnectionType.Internal,
			_ => ConnectionType.Unknown
		};
	}

	public static ConnectionType Convert(Windows.Devices.Display.DisplayMonitorConnectionKind connectionKind, Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind connectorKind)
	{
		var connection = ConvertFromConnection(connectionKind);
		if (connection is ConnectionType.Wired)
		{
			connection = ConvertFromConnector(connectorKind);
		}
		return connection;

		static ConnectionType ConvertFromConnection(Windows.Devices.Display.DisplayMonitorConnectionKind connectionKind)
		{
			return connectionKind switch
			{
				Windows.Devices.Display.DisplayMonitorConnectionKind.Internal => ConnectionType.Internal,
				Windows.Devices.Display.DisplayMonitorConnectionKind.Wired => ConnectionType.Wired,
				Windows.Devices.Display.DisplayMonitorConnectionKind.Wireless => ConnectionType.Wireless,
				Windows.Devices.Display.DisplayMonitorConnectionKind.Virtual => ConnectionType.Virtual,
				_ => ConnectionType.Unknown
			};
		}

		static ConnectionType ConvertFromConnector(Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind connectorKind)
		{
			return connectorKind switch
			{
				Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.HD15 => ConnectionType.VGA,
				Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.AnalogTV => ConnectionType.AnalogTV,
				Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Dvi => ConnectionType.DVI,
				Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Hdmi => ConnectionType.HDMI,
				Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Lvds => ConnectionType.LVDS,
				Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.Sdi => ConnectionType.SDI,
				Windows.Devices.Display.DisplayMonitorPhysicalConnectorKind.DisplayPort => ConnectionType.DisplayPort,
				_ => ConnectionType.Unknown
			};
		}
	}
}