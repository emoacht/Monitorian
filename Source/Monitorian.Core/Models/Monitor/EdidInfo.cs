using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Monitor;

/// <summary>
/// Monitor EDID information
/// </summary>
public class EdidInfo
{
	public string ManufacturerId { get; }
	public string ProductCode { get; }
	public string MonitorName { get; }
	public string SerialNumber { get; }
	public int ManufactureWeek { get; }
	public int ManufactureYear { get; }

	public EdidInfo(in byte[] edid)
	{
		if (edid is not { Length: >= 128 })
			throw new ArgumentException("EDID must be at least 128 bytes.");

		ManufacturerId = GetManufacturerId(edid);
		ProductCode = GetProductCode(edid);
		MonitorName = GetDescriptorData(edid, 0xFC /* Monitor name */);
		SerialNumber = GetDescriptorData(edid, 0xFF /* Monitor serial number */);
		ManufactureWeek = edid[16];
		ManufactureYear = edid[17] + 1990;

		static string GetManufacturerId(in byte[] edid)
		{
			var num = (ushort)(edid[8] * 256 + edid[9]);
			int c1 = ((num / 1024) % 32) + 64;
			int c2 = ((num / 32) % 32) + 64;
			int c3 = (num % 32) + 64;
			return $"{(char)c1}{(char)c2}{(char)c3}";
		}

		static string GetProductCode(in byte[] edid)
		{
			return (edid[10] + edid[11] * 256).ToString("X4");
		}

		static string GetDescriptorData(in byte[] edid, byte descriptorType)
		{
			for (int i = 3; i <= 6; i++)
			{
				int index = i * 18;
				if ((edid[index] == 0) &&
					(edid[index + 1] == 0) &&
					(edid[index + 2] == 0) &&
					(edid[index + 3] == descriptorType))
				{
					return Encoding.ASCII.GetString(edid, index + 5, 13).TrimEnd();
				}
			}
			return null;
		}
	}

	public string Manufacturer
	{
		get => _manufacturerMap.TryGetValue(ManufacturerId, out string name) ? name : ManufacturerId;
	}

	private static readonly Dictionary<string, string> _manufacturerMap = new()
		{
			{ "ACI","ASUS" }, // Verified
			{ "ACR","Acer" }, // Verified
			{ "AOC","AOC" }, // Verified
			{ "APP","Apple" }, // Verified
			{ "AUO","AU Optronics" }, // Verified
			{ "AUS","ASUS" }, // Verified

			{ "BNQ","BenQ" }, // Verified
			{ "BOE","BOE" }, // Verified
						
			{ "DEL","Dell" }, // Verified
						
			{ "ENC","EIZO" }, // Verified

			{ "GBT","Gigabyte" }, // Verified
			{ "GEC","Gechic" }, // Listed
			{ "GSM","LG" }, // Verified

			{ "HKC","KOORUI"}, // Verified
			{ "HPN","HP" }, // Verified
			{ "HWP","HP" }, // Verified

			{ "IOC", "INNOCN" }, // Listed
			{ "IVM","iiyama" }, // Verified

			{ "LEN","Lenovo" }, // Verified
			{ "LGD","LG Display" }, // Verified

			{ "MSI","MSI" }, // Verified

			{ "PHL","Philips" }, // Verified
			{ "PNS","Pixio" }, // Verified
			{ "PXO","Pixio" }, // Listed

			{ "SAM","Samsung" }, // Verified
			{ "SDC","Samsung Display" }, // Verified
			{ "SNY","Sony" }, // Verified
			{ "SPT","Sceptre" }, // Verified

			{ "VSC","ViewSonic" }, // Verified

			{ "XMI","Xiaomi"} // Verified
		};

	public static EdidInfo ReadFromRegistry(string deviceInstanceId)
	{
		var path = $@"SYSTEM\CurrentControlSet\Enum\{deviceInstanceId}\Device Parameters";

		try
		{
			using var key = Registry.LocalMachine.OpenSubKey(path);
			if (key is not null)
			{
				var edid = key.GetValue("EDID") as byte[];
				if (edid is { Length: >= 128 })
					return new EdidInfo(edid);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to read EDID." + Environment.NewLine
				+ ex);
		}
		return null;
	}
}