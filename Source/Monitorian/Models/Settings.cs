using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Monitorian.Common;

namespace Monitorian.Models
{
	/// <summary>
	/// Persistent settings
	/// </summary>
	[DataContract]
	public class Settings : BindableBase
	{
		[DataMember]
		public bool IsLargeElements
		{
			get { return _isLargeElements; }
			set { SetPropertyValue(ref _isLargeElements, value); }
		}
		private bool _isLargeElements = true;

		#region Load/Save

		private const string SettingsFileName = "settings.xml";

		public void Load()
		{
			try
			{
				var filePath = Path.Combine(
					FolderService.GetAppDataFolderPath(false),
					SettingsFileName);

				if (!File.Exists(filePath))
					return;

				using (var sr = new StreamReader(filePath, Encoding.UTF8))
				using (var xr = XmlReader.Create(sr))
				{
					var serializer = new DataContractSerializer(typeof(Settings));
					var loaded = (Settings)serializer.ReadObject(xr);

					typeof(Settings)
						.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
						.Where(x => x.CanWrite)
						.ToList()
						.ForEach(x => x.SetValue(this, x.GetValue(loaded)));
				}
			}
			catch (SerializationException)
			{
				// Ignore broken settings file.
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to load settings from AppData." + Environment.NewLine
					+ ex);
			}
		}

		public void Save()
		{
			try
			{
				var filePath = Path.Combine(
					FolderService.GetAppDataFolderPath(true),
					SettingsFileName);

				using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				using (var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true }))
				{
					var serializer = new DataContractSerializer(typeof(Settings));
					serializer.WriteObject(xw, this);
					xw.Flush();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to save settings to AppData." + Environment.NewLine
					+ ex);
			}
		}

		#endregion
	}
}