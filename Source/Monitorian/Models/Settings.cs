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
using Monitorian.Helper;

namespace Monitorian.Models
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
		public ObservableKeyedList<string, string> KnownMonitors
		{
			get => _knownMonitors ?? (_knownMonitors = new ObservableKeyedList<string, string>());
			private set => _knownMonitors = value;
		}
		private ObservableKeyedList<string, string> _knownMonitors;

		#endregion

		private Throttle _throttle;

		internal void Initiate()
		{
			Load(this);

			_throttle = new Throttle(
				TimeSpan.FromMilliseconds(100),
				() => Save(this));

			KnownMonitors.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(KnownMonitors));
			PropertyChanged += (sender, e) => _throttle.Invoke();
		}

		#region Load/Save

		private const string SettingsFileName = "settings.xml";

		private static void Load<T>(T instance) where T : class
		{
			try
			{
				var filePath = Path.Combine(
					FolderService.GetAppDataFolderPath(false),
					SettingsFileName);

				var fileInfo = new FileInfo(filePath);
				if (!fileInfo.Exists || (fileInfo.Length == 0))
					return;

				using (var sr = new StreamReader(filePath, Encoding.UTF8))
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
				var filePath = Path.Combine(
					FolderService.GetAppDataFolderPath(true),
					SettingsFileName);

				using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
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
}