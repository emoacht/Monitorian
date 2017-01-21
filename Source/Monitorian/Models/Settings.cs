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
		/// <summary>
		/// Whether to use large elements
		/// </summary>
		[DataMember]
		public bool UsesLargeElements
		{
			get { return _usesLargeElements; }
			set { SetPropertyValue(ref _usesLargeElements, value); }
		}
		private bool _usesLargeElements = true;

		/// <summary>
		/// Whether to enable moving together
		/// </summary>
		[DataMember]
		public bool EnablesUnison
		{
			get { return _enablesUnison; }
			set { SetPropertyValue(ref _enablesUnison, value); }
		}
		private bool _enablesUnison = false;

		/// <summary>
		/// Known monitors with user-specified names
		/// </summary>
		[DataMember]
		public Dictionary<string, NamePack> KnownMonitors
		{
			get { return _knownMonitors ?? (_knownMonitors = new Dictionary<string, NamePack>()); }
			private set { _knownMonitors = value; }
		}
		private Dictionary<string, NamePack> _knownMonitors;

		#region Load/Save

		private const string SettingsFileName = "settings.xml";

		internal void Load()
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

		internal void Save()
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

	[DataContract]
	public class NamePack
	{
		/// <summary>
		/// Name
		/// </summary>
		/// <remarks>Private setter is for serializer.</remarks>
		[DataMember]
		public string Name { get; private set; }

		/// <summary>
		/// Instantiation time
		/// </summary>
		/// <remarks>Private setter is for serializer.</remarks>
		[DataMember]
		public long Time { get; private set; }

		public NamePack(string name)
		{
			this.Name = name;
			Time = DateTimeOffset.UtcNow.Ticks;
		}
	}
}