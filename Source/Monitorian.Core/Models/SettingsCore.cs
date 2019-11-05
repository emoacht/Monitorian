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
	public class SettingsCore : BindableBase
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
			get => _knownMonitors ??= new ObservableKeyedList<string, MonitorValuePack>();
			protected set => _knownMonitors = value;
		}
		private ObservableKeyedList<string, MonitorValuePack> _knownMonitors;

		/// <summary>
		/// Device Instance ID of selected monitor
		/// </summary>
		[DataMember]
		public string SelectedDeviceInstanceId
		{
			get => _selectedDeviceInstanceId;
			set => SetPropertyValue(ref _selectedDeviceInstanceId, value);
		}
		private string _selectedDeviceInstanceId;

		#endregion

		private const string SettingsFileName = "settings.xml";
		private readonly string _settingsFilePath;

		public SettingsCore() : this(null)
		{ }

		protected SettingsCore(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				fileName = SettingsFileName;

			_settingsFilePath = Path.Combine(FolderService.AppDataFolderPath, fileName);
		}

		private Throttle _save;

		protected internal virtual void Initiate()
		{
			Load(this, _settingsFilePath);

			_save = new Throttle(
				TimeSpan.FromMilliseconds(100),
				() => Save(this, _settingsFilePath));

			KnownMonitors.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(KnownMonitors));
			PropertyChanged += async (sender, e) => await _save.PushAsync();
		}

		#region Load/Save

		private static void Load<T>(T instance, string filePath) where T : class
		{
			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists || (fileInfo.Length == 0))
				return;

			var type = instance.GetType(); // GetType method works in derived class.

			try
			{
				using (var sr = new StreamReader(filePath, Encoding.UTF8))
				using (var xr = XmlReader.Create(sr))
				{
					var serializer = new DataContractSerializer(type);
					var loaded = (T)serializer.ReadObject(xr);

					type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
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

		private static void Save<T>(T instance, string filePath) where T : class
		{
			var type = instance.GetType(); // GetType method works in derived class.

			try
			{
				FolderService.AssureAppDataFolder();

				using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				using (var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true }))
				{
					var serializer = new DataContractSerializer(type);
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