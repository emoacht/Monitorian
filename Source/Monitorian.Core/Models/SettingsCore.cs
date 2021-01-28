﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Monitorian.Core.Collections;
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
		/// Whether to show adjusted brightness
		/// </summary>
		[DataMember]
		public bool ShowsAdjusted
		{
			get => _showsAdjusted;
			set => SetPropertyValue(ref _showsAdjusted, value);
		}
		private bool _showsAdjusted = true;

		/// <summary>
		/// Whether to enable moving in unison
		/// </summary>
		[DataMember]
		public bool EnablesUnison
		{
			get => _enablesUnison;
			set => SetPropertyValue(ref _enablesUnison, value);
		}
		private bool _enablesUnison = false;

		/// <summary>
		/// Whether to change adjustable range
		/// </summary>
		[DataMember]
		public bool ChangesRange
		{
			get => _changesRange;
			set => SetPropertyValue(ref _changesRange, value);
		}
		private bool _changesRange;

		/// <summary>
		/// Monitor customizations by user
		/// </summary>
		[DataMember]
		public ObservableKeyedList<string, MonitorCustomizationItem> MonitorCustomizations
		{
			get => _monitorCustomizations ??= new ObservableKeyedList<string, MonitorCustomizationItem>();
			protected set => _monitorCustomizations = value;
		}
		private ObservableKeyedList<string, MonitorCustomizationItem> _monitorCustomizations;

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

		/// <summary>
		/// Whether to make operation log
		/// </summary>
		[DataMember]
		public bool MakesOperationLog
		{
			get => _makesOperationLog;
			set => SetPropertyValue(ref _makesOperationLog, value);
		}
		private bool _makesOperationLog;

		#endregion

		private const string SettingsFileName = "settings.xml";
		private readonly string _fileName;

		public SettingsCore() : this(null)
		{ }

		protected SettingsCore(string fileName)
		{
			this._fileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : SettingsFileName;
		}

		private Throttle _save;

		protected internal virtual void Initiate()
		{
			Load(this);

			_save = new Throttle(
				TimeSpan.FromMilliseconds(100),
				() => Save(this));

			MonitorCustomizations.CollectionChanged += (sender, e) => RaisePropertyChanged(nameof(MonitorCustomizations));
			PropertyChanged += async (sender, e) => await _save.PushAsync();
		}

		#region Load/Save

		private void Load<T>(T instance) where T : class
		{
			try
			{
				AppDataService.Load(instance, _fileName);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to load settings from AppData." + Environment.NewLine
					+ ex);
			}
		}

		private void Save<T>(T instance) where T : class
		{
			try
			{
				AppDataService.Save(instance, _fileName);
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
	public class MonitorCustomizationItem
	{
		[DataMember]
		public string Name { get; private set; }

		[DataMember(Name = "Unison")]
		public bool IsUnison { get; private set; }

		[DataMember]
		public byte Lowest { get; private set; } = 0;

		[DataMember]
		public byte Highest { get; private set; } = 100;

		public MonitorCustomizationItem(string name, bool isUnison)
		{
			this.Name = name;
			this.IsUnison = isUnison;
		}

		public MonitorCustomizationItem(string name, bool isUnison, byte lowest, byte highest) : this(name, isUnison)
		{
			this.Lowest = lowest;
			this.Highest = highest;
		}
	}
}