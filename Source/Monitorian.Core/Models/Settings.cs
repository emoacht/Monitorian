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

using Monitorian.Core.Common;
using Monitorian.Core.Helper;

namespace Monitorian.Core.Models
{
	/// <summary>
	/// Persistent settings
	/// </summary>
	[DataContract]
	public class Settings : BindableBase
	{
		#region Settings

		/// <summary>
		/// Whether to use large elements
		/// </summary>
		[DataMember]
		public bool UsesLargeElements
		{
			get => _usesLargeElements;
			set => SetPropertyValue(ref _usesLargeElements, value);
		}
		private bool _usesLargeElements = true; // Default

		/// <summary>
		/// Whether to enable moving together
		/// </summary>
		[DataMember]
		public bool EnablesUnison
		{
			get => _enablesUnison;
			set => SetPropertyValue(ref _enablesUnison, value);
		}
		private bool _enablesUnison = false;

		/// <summary>
		/// Whether to show adjusted brightness
		/// </summary>
		[DataMember]
		public bool ShowsAdjusted
		{
			get => _showsAdjusted;
			set => SetPropertyValue(ref _showsAdjusted, value);
		}
		private bool _showsAdjusted = false;

		/// <summary>
		/// Known monitors with user-specified names
		/// </summary>
		[DataMember]
		public ObservableKeyedList<string, MonitorValuePack> KnownMonitors
		{
			get => _knownMonitors ?? (_knownMonitors = new ObservableKeyedList<string, MonitorValuePack>());
			private set => _knownMonitors = value;
		}
		private ObservableKeyedList<string, MonitorValuePack> _knownMonitors;

		#endregion

		private Throttle _throttle;

		internal void Initiate()
		{
			Load(this);

			_throttle = new Throttle(
				TimeSpan.FromMilliseconds(100),
				() => Save(this));

			KnownMonitors.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(KnownMonitors));
			PropertyChanged += async (sender, e) => await _throttle.PushAsync();
		}

		#region Load/Save

		private const string SettingsFileName = "settings.xml";
		private static readonly string _settingsFilePath = Path.Combine(FolderService.AppDataFolderPath, SettingsFileName);

		private static void Load<T>(T instance) where T : class
		{
			var fileInfo = new FileInfo(_settingsFilePath);
			if (!fileInfo.Exists || (fileInfo.Length == 0))
				return;

			try
			{
				using (var sr = new StreamReader(_settingsFilePath, Encoding.UTF8))
				using (var xr = XmlReader.Create(sr))
				{
					var serializer = new DataContractSerializer(typeof(T));
					var loaded = (T)serializer.ReadObject(xr);

					typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
						.Where(x => x.CanWrite)
						.ToList()
						.ForEach(x => x.SetValue(instance, x.GetValue(loaded)));
				}
			}
			catch (Exception ex)
			{
				if ((ex is SerializationException) | (ex is XmlException))
				{
					// Ignore faulty settings file.
					return;
				}

				Debug.WriteLine("Failed to load settings from AppData." + Environment.NewLine
					+ ex);
			}
		}

		private static void Save<T>(T instance) where T : class
		{
			try
			{
				FolderService.AssureAppDataFolder();

				using (var sw = new StreamWriter(_settingsFilePath, false, Encoding.UTF8)) // BOM will be emitted.
				using (var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true }))
				{
					var serializer = new DataContractSerializer(typeof(T));
					serializer.WriteObject(xw, instance);
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
	public class MonitorValuePack
	{
		[DataMember]
		public string Name { get; private set; }

		[DataMember(Name = "Unison")]
		public bool IsUnison { get; private set; }

		public MonitorValuePack(string name, bool isUnison)
		{
			this.Name = name;
			this.IsUnison = isUnison;
		}
	}
}